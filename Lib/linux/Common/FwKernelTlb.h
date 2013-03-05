

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0555 */
/* at Tue Mar 05 11:51:28 2013
 */
/* Compiler settings for d:\fwrepo\fw\Output\Common\FwKernelTlb.idl:
	Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 7.00.0555
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

#ifndef __ITsString_FWD_DEFINED__
#define __ITsString_FWD_DEFINED__
typedef interface ITsString ITsString;
#endif 	/* __ITsString_FWD_DEFINED__ */


#ifndef __IUndoGrouper_FWD_DEFINED__
#define __IUndoGrouper_FWD_DEFINED__
typedef interface IUndoGrouper IUndoGrouper;
#endif 	/* __IUndoGrouper_FWD_DEFINED__ */


#ifndef __IFwMetaDataCache_FWD_DEFINED__
#define __IFwMetaDataCache_FWD_DEFINED__
typedef interface IFwMetaDataCache IFwMetaDataCache;
#endif 	/* __IFwMetaDataCache_FWD_DEFINED__ */


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


#ifndef __IDebugReportSink_FWD_DEFINED__
#define __IDebugReportSink_FWD_DEFINED__
typedef interface IDebugReportSink IDebugReportSink;
#endif 	/* __IDebugReportSink_FWD_DEFINED__ */


#ifndef __IDebugReport_FWD_DEFINED__
#define __IDebugReport_FWD_DEFINED__
typedef interface IDebugReport IDebugReport;
#endif 	/* __IDebugReport_FWD_DEFINED__ */


#ifndef __DebugReport_FWD_DEFINED__
#define __DebugReport_FWD_DEFINED__

#ifdef __cplusplus
typedef class DebugReport DebugReport;
#else
typedef struct DebugReport DebugReport;
#endif /* __cplusplus */

#endif 	/* __DebugReport_FWD_DEFINED__ */


#ifndef __IComDisposable_FWD_DEFINED__
#define __IComDisposable_FWD_DEFINED__
typedef interface IComDisposable IComDisposable;
#endif 	/* __IComDisposable_FWD_DEFINED__ */


#ifndef __ITsTextProps_FWD_DEFINED__
#define __ITsTextProps_FWD_DEFINED__
typedef interface ITsTextProps ITsTextProps;
#endif 	/* __ITsTextProps_FWD_DEFINED__ */


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


#ifndef __ITsMultiString_FWD_DEFINED__
#define __ITsMultiString_FWD_DEFINED__
typedef interface ITsMultiString ITsMultiString;
#endif 	/* __ITsMultiString_FWD_DEFINED__ */


#ifndef __ILgWritingSystemFactory_FWD_DEFINED__
#define __ILgWritingSystemFactory_FWD_DEFINED__
typedef interface ILgWritingSystemFactory ILgWritingSystemFactory;
#endif 	/* __ILgWritingSystemFactory_FWD_DEFINED__ */


#ifndef __TsStrFactory_FWD_DEFINED__
#define __TsStrFactory_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsStrFactory TsStrFactory;
#else
typedef struct TsStrFactory TsStrFactory;
#endif /* __cplusplus */

#endif 	/* __TsStrFactory_FWD_DEFINED__ */


#ifndef __TsPropsFactory_FWD_DEFINED__
#define __TsPropsFactory_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsPropsFactory TsPropsFactory;
#else
typedef struct TsPropsFactory TsPropsFactory;
#endif /* __cplusplus */

#endif 	/* __TsPropsFactory_FWD_DEFINED__ */


#ifndef __TsStrBldr_FWD_DEFINED__
#define __TsStrBldr_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsStrBldr TsStrBldr;
#else
typedef struct TsStrBldr TsStrBldr;
#endif /* __cplusplus */

#endif 	/* __TsStrBldr_FWD_DEFINED__ */


#ifndef __TsIncStrBldr_FWD_DEFINED__
#define __TsIncStrBldr_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsIncStrBldr TsIncStrBldr;
#else
typedef struct TsIncStrBldr TsIncStrBldr;
#endif /* __cplusplus */

#endif 	/* __TsIncStrBldr_FWD_DEFINED__ */


#ifndef __TsPropsBldr_FWD_DEFINED__
#define __TsPropsBldr_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsPropsBldr TsPropsBldr;
#else
typedef struct TsPropsBldr TsPropsBldr;
#endif /* __cplusplus */

#endif 	/* __TsPropsBldr_FWD_DEFINED__ */


#ifndef __TsMultiString_FWD_DEFINED__
#define __TsMultiString_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsMultiString TsMultiString;
#else
typedef struct TsMultiString TsMultiString;
#endif /* __cplusplus */

#endif 	/* __TsMultiString_FWD_DEFINED__ */


#ifndef __ILgInputMethodEditor_FWD_DEFINED__
#define __ILgInputMethodEditor_FWD_DEFINED__
typedef interface ILgInputMethodEditor ILgInputMethodEditor;
#endif 	/* __ILgInputMethodEditor_FWD_DEFINED__ */


#ifndef __IVwGraphics_FWD_DEFINED__
#define __IVwGraphics_FWD_DEFINED__
typedef interface IVwGraphics IVwGraphics;
#endif 	/* __IVwGraphics_FWD_DEFINED__ */


#ifndef __IJustifyingRenderer_FWD_DEFINED__
#define __IJustifyingRenderer_FWD_DEFINED__
typedef interface IJustifyingRenderer IJustifyingRenderer;
#endif 	/* __IJustifyingRenderer_FWD_DEFINED__ */


#ifndef __ISimpleInit_FWD_DEFINED__
#define __ISimpleInit_FWD_DEFINED__
typedef interface ISimpleInit ISimpleInit;
#endif 	/* __ISimpleInit_FWD_DEFINED__ */


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


#ifndef __VwJustifier_FWD_DEFINED__
#define __VwJustifier_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwJustifier VwJustifier;
#else
typedef struct VwJustifier VwJustifier;
#endif /* __cplusplus */

#endif 	/* __VwJustifier_FWD_DEFINED__ */


#ifndef __ILgSegment_FWD_DEFINED__
#define __ILgSegment_FWD_DEFINED__
typedef interface ILgSegment ILgSegment;
#endif 	/* __ILgSegment_FWD_DEFINED__ */


#ifndef __IRenderEngine_FWD_DEFINED__
#define __IRenderEngine_FWD_DEFINED__
typedef interface IRenderEngine IRenderEngine;
#endif 	/* __IRenderEngine_FWD_DEFINED__ */


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


#ifndef __FwGrEngine_FWD_DEFINED__
#define __FwGrEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwGrEngine FwGrEngine;
#else
typedef struct FwGrEngine FwGrEngine;
#endif /* __cplusplus */

#endif 	/* __FwGrEngine_FWD_DEFINED__ */


#ifndef __IRenderingFeatures_FWD_DEFINED__
#define __IRenderingFeatures_FWD_DEFINED__
typedef interface IRenderingFeatures IRenderingFeatures;
#endif 	/* __IRenderingFeatures_FWD_DEFINED__ */


#ifndef __FwGraphiteProcess_FWD_DEFINED__
#define __FwGraphiteProcess_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwGraphiteProcess FwGraphiteProcess;
#else
typedef struct FwGraphiteProcess FwGraphiteProcess;
#endif /* __cplusplus */

#endif 	/* __FwGraphiteProcess_FWD_DEFINED__ */


#ifndef __ILgCharacterPropertyEngine_FWD_DEFINED__
#define __ILgCharacterPropertyEngine_FWD_DEFINED__
typedef interface ILgCharacterPropertyEngine ILgCharacterPropertyEngine;
#endif 	/* __ILgCharacterPropertyEngine_FWD_DEFINED__ */


#ifndef __ILgStringConverter_FWD_DEFINED__
#define __ILgStringConverter_FWD_DEFINED__
typedef interface ILgStringConverter ILgStringConverter;
#endif 	/* __ILgStringConverter_FWD_DEFINED__ */


#ifndef __ILgTokenizer_FWD_DEFINED__
#define __ILgTokenizer_FWD_DEFINED__
typedef interface ILgTokenizer ILgTokenizer;
#endif 	/* __ILgTokenizer_FWD_DEFINED__ */


#ifndef __ILgSpellCheckFactory_FWD_DEFINED__
#define __ILgSpellCheckFactory_FWD_DEFINED__
typedef interface ILgSpellCheckFactory ILgSpellCheckFactory;
#endif 	/* __ILgSpellCheckFactory_FWD_DEFINED__ */


#ifndef __ILgSpellChecker_FWD_DEFINED__
#define __ILgSpellChecker_FWD_DEFINED__
typedef interface ILgSpellChecker ILgSpellChecker;
#endif 	/* __ILgSpellChecker_FWD_DEFINED__ */


#ifndef __ILgCollatingEngine_FWD_DEFINED__
#define __ILgCollatingEngine_FWD_DEFINED__
typedef interface ILgCollatingEngine ILgCollatingEngine;
#endif 	/* __ILgCollatingEngine_FWD_DEFINED__ */


#ifndef __ILgSearchEngine_FWD_DEFINED__
#define __ILgSearchEngine_FWD_DEFINED__
typedef interface ILgSearchEngine ILgSearchEngine;
#endif 	/* __ILgSearchEngine_FWD_DEFINED__ */


#ifndef __ILgWritingSystem_FWD_DEFINED__
#define __ILgWritingSystem_FWD_DEFINED__
typedef interface ILgWritingSystem ILgWritingSystem;
#endif 	/* __ILgWritingSystem_FWD_DEFINED__ */


#ifndef __ILgTextServices_FWD_DEFINED__
#define __ILgTextServices_FWD_DEFINED__
typedef interface ILgTextServices ILgTextServices;
#endif 	/* __ILgTextServices_FWD_DEFINED__ */


#ifndef __ILgFontManager_FWD_DEFINED__
#define __ILgFontManager_FWD_DEFINED__
typedef interface ILgFontManager ILgFontManager;
#endif 	/* __ILgFontManager_FWD_DEFINED__ */


#ifndef __LgInputMethodEditor_FWD_DEFINED__
#define __LgInputMethodEditor_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgInputMethodEditor LgInputMethodEditor;
#else
typedef struct LgInputMethodEditor LgInputMethodEditor;
#endif /* __cplusplus */

#endif 	/* __LgInputMethodEditor_FWD_DEFINED__ */


#ifndef __LgFontManager_FWD_DEFINED__
#define __LgFontManager_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgFontManager LgFontManager;
#else
typedef struct LgFontManager LgFontManager;
#endif /* __cplusplus */

#endif 	/* __LgFontManager_FWD_DEFINED__ */


#ifndef __ILgIcuCharPropEngine_FWD_DEFINED__
#define __ILgIcuCharPropEngine_FWD_DEFINED__
typedef interface ILgIcuCharPropEngine ILgIcuCharPropEngine;
#endif 	/* __ILgIcuCharPropEngine_FWD_DEFINED__ */


#ifndef __ILgNumericEngine_FWD_DEFINED__
#define __ILgNumericEngine_FWD_DEFINED__
typedef interface ILgNumericEngine ILgNumericEngine;
#endif 	/* __ILgNumericEngine_FWD_DEFINED__ */


#ifndef __ILgKeymanHandler_FWD_DEFINED__
#define __ILgKeymanHandler_FWD_DEFINED__
typedef interface ILgKeymanHandler ILgKeymanHandler;
#endif 	/* __ILgKeymanHandler_FWD_DEFINED__ */


#ifndef __ILgCodePageEnumerator_FWD_DEFINED__
#define __ILgCodePageEnumerator_FWD_DEFINED__
typedef interface ILgCodePageEnumerator ILgCodePageEnumerator;
#endif 	/* __ILgCodePageEnumerator_FWD_DEFINED__ */


#ifndef __ILgLanguageEnumerator_FWD_DEFINED__
#define __ILgLanguageEnumerator_FWD_DEFINED__
typedef interface ILgLanguageEnumerator ILgLanguageEnumerator;
#endif 	/* __ILgLanguageEnumerator_FWD_DEFINED__ */


#ifndef __ILgIcuConverterEnumerator_FWD_DEFINED__
#define __ILgIcuConverterEnumerator_FWD_DEFINED__
typedef interface ILgIcuConverterEnumerator ILgIcuConverterEnumerator;
#endif 	/* __ILgIcuConverterEnumerator_FWD_DEFINED__ */


#ifndef __ILgIcuTransliteratorEnumerator_FWD_DEFINED__
#define __ILgIcuTransliteratorEnumerator_FWD_DEFINED__
typedef interface ILgIcuTransliteratorEnumerator ILgIcuTransliteratorEnumerator;
#endif 	/* __ILgIcuTransliteratorEnumerator_FWD_DEFINED__ */


#ifndef __ILgIcuLocaleEnumerator_FWD_DEFINED__
#define __ILgIcuLocaleEnumerator_FWD_DEFINED__
typedef interface ILgIcuLocaleEnumerator ILgIcuLocaleEnumerator;
#endif 	/* __ILgIcuLocaleEnumerator_FWD_DEFINED__ */


#ifndef __ILgIcuResourceBundle_FWD_DEFINED__
#define __ILgIcuResourceBundle_FWD_DEFINED__
typedef interface ILgIcuResourceBundle ILgIcuResourceBundle;
#endif 	/* __ILgIcuResourceBundle_FWD_DEFINED__ */


#ifndef __IRegexMatcher_FWD_DEFINED__
#define __IRegexMatcher_FWD_DEFINED__
typedef interface IRegexMatcher IRegexMatcher;
#endif 	/* __IRegexMatcher_FWD_DEFINED__ */


#ifndef __RegexMatcherWrapper_FWD_DEFINED__
#define __RegexMatcherWrapper_FWD_DEFINED__

#ifdef __cplusplus
typedef class RegexMatcherWrapper RegexMatcherWrapper;
#else
typedef struct RegexMatcherWrapper RegexMatcherWrapper;
#endif /* __cplusplus */

#endif 	/* __RegexMatcherWrapper_FWD_DEFINED__ */


#ifndef __LgSystemCollater_FWD_DEFINED__
#define __LgSystemCollater_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgSystemCollater LgSystemCollater;
#else
typedef struct LgSystemCollater LgSystemCollater;
#endif /* __cplusplus */

#endif 	/* __LgSystemCollater_FWD_DEFINED__ */


#ifndef __LgUnicodeCollater_FWD_DEFINED__
#define __LgUnicodeCollater_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgUnicodeCollater LgUnicodeCollater;
#else
typedef struct LgUnicodeCollater LgUnicodeCollater;
#endif /* __cplusplus */

#endif 	/* __LgUnicodeCollater_FWD_DEFINED__ */


#ifndef __LgIcuCollator_FWD_DEFINED__
#define __LgIcuCollator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuCollator LgIcuCollator;
#else
typedef struct LgIcuCollator LgIcuCollator;
#endif /* __cplusplus */

#endif 	/* __LgIcuCollator_FWD_DEFINED__ */


#ifndef __LgIcuCharPropEngine_FWD_DEFINED__
#define __LgIcuCharPropEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuCharPropEngine LgIcuCharPropEngine;
#else
typedef struct LgIcuCharPropEngine LgIcuCharPropEngine;
#endif /* __cplusplus */

#endif 	/* __LgIcuCharPropEngine_FWD_DEFINED__ */


#ifndef __LgCPWordTokenizer_FWD_DEFINED__
#define __LgCPWordTokenizer_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgCPWordTokenizer LgCPWordTokenizer;
#else
typedef struct LgCPWordTokenizer LgCPWordTokenizer;
#endif /* __cplusplus */

#endif 	/* __LgCPWordTokenizer_FWD_DEFINED__ */


#ifndef __LgWfiSpellChecker_FWD_DEFINED__
#define __LgWfiSpellChecker_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgWfiSpellChecker LgWfiSpellChecker;
#else
typedef struct LgWfiSpellChecker LgWfiSpellChecker;
#endif /* __cplusplus */

#endif 	/* __LgWfiSpellChecker_FWD_DEFINED__ */


#ifndef __LgMSWordSpellChecker_FWD_DEFINED__
#define __LgMSWordSpellChecker_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgMSWordSpellChecker LgMSWordSpellChecker;
#else
typedef struct LgMSWordSpellChecker LgMSWordSpellChecker;
#endif /* __cplusplus */

#endif 	/* __LgMSWordSpellChecker_FWD_DEFINED__ */


#ifndef __LgNumericEngine_FWD_DEFINED__
#define __LgNumericEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgNumericEngine LgNumericEngine;
#else
typedef struct LgNumericEngine LgNumericEngine;
#endif /* __cplusplus */

#endif 	/* __LgNumericEngine_FWD_DEFINED__ */


#ifndef __LgKeymanHandler_FWD_DEFINED__
#define __LgKeymanHandler_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgKeymanHandler LgKeymanHandler;
#else
typedef struct LgKeymanHandler LgKeymanHandler;
#endif /* __cplusplus */

#endif 	/* __LgKeymanHandler_FWD_DEFINED__ */


#ifndef __LgTextServices_FWD_DEFINED__
#define __LgTextServices_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgTextServices LgTextServices;
#else
typedef struct LgTextServices LgTextServices;
#endif /* __cplusplus */

#endif 	/* __LgTextServices_FWD_DEFINED__ */


#ifndef __LgCodePageEnumerator_FWD_DEFINED__
#define __LgCodePageEnumerator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgCodePageEnumerator LgCodePageEnumerator;
#else
typedef struct LgCodePageEnumerator LgCodePageEnumerator;
#endif /* __cplusplus */

#endif 	/* __LgCodePageEnumerator_FWD_DEFINED__ */


#ifndef __LgLanguageEnumerator_FWD_DEFINED__
#define __LgLanguageEnumerator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgLanguageEnumerator LgLanguageEnumerator;
#else
typedef struct LgLanguageEnumerator LgLanguageEnumerator;
#endif /* __cplusplus */

#endif 	/* __LgLanguageEnumerator_FWD_DEFINED__ */


#ifndef __LgIcuConverterEnumerator_FWD_DEFINED__
#define __LgIcuConverterEnumerator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuConverterEnumerator LgIcuConverterEnumerator;
#else
typedef struct LgIcuConverterEnumerator LgIcuConverterEnumerator;
#endif /* __cplusplus */

#endif 	/* __LgIcuConverterEnumerator_FWD_DEFINED__ */


#ifndef __LgIcuTransliteratorEnumerator_FWD_DEFINED__
#define __LgIcuTransliteratorEnumerator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuTransliteratorEnumerator LgIcuTransliteratorEnumerator;
#else
typedef struct LgIcuTransliteratorEnumerator LgIcuTransliteratorEnumerator;
#endif /* __cplusplus */

#endif 	/* __LgIcuTransliteratorEnumerator_FWD_DEFINED__ */


#ifndef __LgIcuResourceBundle_FWD_DEFINED__
#define __LgIcuResourceBundle_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuResourceBundle LgIcuResourceBundle;
#else
typedef struct LgIcuResourceBundle LgIcuResourceBundle;
#endif /* __cplusplus */

#endif 	/* __LgIcuResourceBundle_FWD_DEFINED__ */


#ifndef __LgIcuLocaleEnumerator_FWD_DEFINED__
#define __LgIcuLocaleEnumerator_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuLocaleEnumerator LgIcuLocaleEnumerator;
#else
typedef struct LgIcuLocaleEnumerator LgIcuLocaleEnumerator;
#endif /* __cplusplus */

#endif 	/* __LgIcuLocaleEnumerator_FWD_DEFINED__ */


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




typedef /* [v1_enum] */
enum UndoResult
	{	kuresSuccess	= 0,
	kuresRefresh	= ( kuresSuccess + 1 ) ,
	kuresFailed	= ( kuresRefresh + 1 ) ,
	kuresError	= ( kuresFailed + 1 )
	} 	UndoResult;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IUndoAction
,
B831F535-0D5F-42c8-BF9F-7F5ECA2C4657
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IActionHandler
,
0F8EA3BE-C982-40f8-B674-25B8482EB222
);
ATTACH_GUID_TO_CLASS(class,
6A46D810-7F14-4151-80F5-0B13FFC1F917
,
ActionHandler
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwMetaDataCache
,
EDBB1DED-7065-4b56-A262-746453835451
);
typedef /* [v1_enum] */
enum CrtReportType
	{	Warn	= 0,
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
ATTACH_GUID_TO_CLASS(class,
24636FD1-DB8D-4b2c-B4C0-44C2592CA482
,
DebugReport
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IUndoGrouper
,
C38348D3-392C-4e02-BD50-A01DC4189E1D
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
	{	ktpvDefault	= 0,
	ktpvMilliPoint	= 0x1,
	ktpvRelative	= 0x2,
	ktpvEnum	= 0x3,
	ktpvNinch	= 0xf
	} 	FwTextPropVar;

typedef /* [v1_enum] */
enum FwNormalizationMode
	{	knmNone	= 1,
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
	{	ktptWs	= 1,
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
	{	ktptNotEditable	= 0,
	ktptIsEditable	= ( ktptNotEditable + 1 ) ,
	ktptSemiEditable	= ( ktptIsEditable + 1 )
	} 	TptEditable;

typedef /* [v1_enum] */
enum SpellingModes
	{	ksmMin	= 0,
	ksmNormalCheck	= 0,
	ksmDoNotCheck	= ( ksmNormalCheck + 1 ) ,
	ksmForceCheck	= ( ksmDoNotCheck + 1 ) ,
	ksmLim	= ( ksmForceCheck + 1 )
	} 	SpellingModes;

typedef /* [v1_enum] */
enum FwObjDataTypes
	{	kodtPictEvenHot	= 1,
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
	{	kscpWs	= ( ( ktptWs << 2 )  | 2 ) ,
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
	{	kstpFontFamily	= ktptFontFamily,
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
	{	kdenTextPropRel	= 10000,
	kcbitTextPropVar	= 4,
	knNinch	= 0x80000000,
	knConflicting	= 0x80000001
	} 	FwTextPropConstants;

typedef /* [v1_enum] */
enum FwTextToggleVal
	{	kttvOff	= 0,
	kttvForceOn	= 1,
	kttvInvert	= 2
	} 	FwTextToggleVal;

typedef /* [v1_enum] */
enum FwSuperscriptVal
	{	kssvOff	= 0,
	kssvSuper	= 1,
	kssvSub	= 2
	} 	FwSuperscriptVal;

typedef /* [v1_enum] */
enum FwTextColor
	{	kclrWhite	= 0xffffff,
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
	{	kuntMin	= 0,
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
	{	ktalMin	= 0,
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
295B2E11-B149-49C5-9BE9-9F46185609AA
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsTextProps
,
4FA0B99A-5A56-41A4-BE8B-B89BC62251A5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStrFactory
,
C10EA417-8317-4048-AC90-103F8BDFB325
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsPropsFactory
,
8DCE56A6-CFF1-4402-95FE-2B574912B54E
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStrBldr
,
F1EF76E6-BE04-11d3-8D9A-005004DEFEC4
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsIncStrBldr
,
F1EF76E7-BE04-11d3-8D9A-005004DEFEC4
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
ATTACH_GUID_TO_CLASS(class,
F1EF76E9-BE04-11d3-8D9A-005004DEFEC4
,
TsStrFactory
);
ATTACH_GUID_TO_CLASS(class,
F1EF76EA-BE04-11d3-8D9A-005004DEFEC4
,
TsPropsFactory
);
ATTACH_GUID_TO_CLASS(class,
F1EF76EB-BE04-11d3-8D9A-005004DEFEC4
,
TsStrBldr
);
ATTACH_GUID_TO_CLASS(class,
F1EF76EC-BE04-11d3-8D9A-005004DEFEC4
,
TsIncStrBldr
);
ATTACH_GUID_TO_CLASS(class,
F1EF76ED-BE04-11d3-8D9A-005004DEFEC4
,
TsPropsBldr
);
ATTACH_GUID_TO_CLASS(class,
7A1B89C0-C2D6-11d3-9BB7-00400541F9E9
,
TsMultiString
);





typedef /* [v1_enum] */
enum LgLineBreak
	{	klbNoBreak	= 0,
	klbWsBreak	= 10,
	klbWordBreak	= 15,
	klbGoodBreak	= 19,
	klbHyphenBreak	= 20,
	klbLetterBreak	= 30,
	klbClipBreak	= 40
	} 	LgLineBreak;

typedef /* [v1_enum] */
enum LgLineBreakStatus
	{	kflbsBrk	= 0x1,
	kflbsSpace	= 0x2,
	kflbsBrkL	= 0x4
	} 	LgLineBreakStatus;

typedef /* [v1_enum] */
enum LgIPDrawMode
	{	kdmNormal	= 0,
	kdmSplitPrimary	= ( kdmNormal + 1 ) ,
	kdmSplitSecondary	= ( kdmSplitPrimary + 1 )
	} 	LgIPDrawMode;

typedef /* [v1_enum] */
enum LgIpValidResult
	{	kipvrOK	= 0,
	kipvrBad	= ( kipvrOK + 1 ) ,
	kipvrUnknown	= ( kipvrBad + 1 )
	} 	LgIpValidResult;

typedef /* [v1_enum] */
enum LgTrailingWsHandling
	{	ktwshAll	= 0,
	ktwshNoWs	= ( ktwshAll + 1 ) ,
	ktwshOnlyWs	= ( ktwshNoWs + 1 )
	} 	LgTrailingWsHandling;

typedef /* [v1_enum] */
enum LgUtfForm
	{	kutf8	= 0,
	kutf16	= ( kutf8 + 1 ) ,
	kutf32	= ( kutf16 + 1 )
	} 	LgUtfForm;

typedef /* [v1_enum] */
enum VwGenericFontNames
	{	kvgfnCustom	= 0,
	kvgfnSerif	= ( kvgfnCustom + 1 ) ,
	kvgfnSansSerif	= ( kvgfnSerif + 1 ) ,
	kvgfnMonospace	= ( kvgfnSansSerif + 1 )
	} 	VwGenericFontNames;

typedef /* [v1_enum] */
enum VwFontStyle
	{	kfsNormal	= 0,
	kfsItalic	= ( kfsNormal + 1 ) ,
	kfsOblique	= ( kfsItalic + 1 )
	} 	VwFontStyle;

typedef /* [v1_enum] */
enum VwTextUnderline
	{	ktuNoUnderline	= 0,
	ktuSingleUnderline	= ( ktuNoUnderline + 1 )
	} 	VwTextUnderline;

typedef /* [public][public][public][public][public][public] */ struct __MIDL___MIDL_itf_FwKernelTlb_0001_0079_0001
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

typedef
enum ScriptDirCode
	{	kfsdcNone	= 0,
	kfsdcHorizLtr	= 1,
	kfsdcHorizRtl	= 2,
	kfsdcVertFromLeft	= 4,
	kfsdcVertFromRight	= 8
	} 	ScriptDirCode;

typedef
enum JustGlyphAttr
	{	kjgatStretch	= 1,
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

typedef /* [public][public] */ struct __MIDL___MIDL_itf_FwKernelTlb_0001_0079_0002
	{
	ScriptDirCode sdcPara;
	ScriptDirCode sdcOuter;
	} 	LgParaRenderProps;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISimpleInit
,
6433D19E-2DA2-4041-B202-DB118EE1694D
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
ATTACH_GUID_TO_CLASS(class,
D3E3ADB7-94CB-443B-BB8F-82A03BF850F3
,
VwJustifier
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSegment
,
3818E245-6A0B-45A7-A5D6-52694931279E
);
typedef /* [public][public][v1_enum] */
enum __MIDL___MIDL_itf_FwKernelTlb_0001_0085_0001
	{	kestNoMore	= 0,
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
7F4B8F79-2A40-408C-944B-848B14D65D23
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
F39F9433-F05A-4A19-8D1E-3C55DD607633
,
FwGrEngine
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
ATTACH_GUID_TO_CLASS(class,
B56AEFB9-96B4-4415-8415-64CBF3826704
,
FwGraphiteProcess
);












typedef /* [v1_enum] */
enum LgGeneralCharCategory
	{	kccLu	= 0,
	kccLl	= ( kccLu + 1 ) ,
	kccLt	= ( kccLl + 1 ) ,
	kccLm	= ( kccLt + 1 ) ,
	kccLo	= ( kccLm + 1 ) ,
	kccMn	= ( kccLo + 1 ) ,
	kccMc	= ( kccMn + 1 ) ,
	kccMe	= ( kccMc + 1 ) ,
	kccNd	= ( kccMe + 1 ) ,
	kccNl	= ( kccNd + 1 ) ,
	kccNo	= ( kccNl + 1 ) ,
	kccZs	= ( kccNo + 1 ) ,
	kccZl	= ( kccZs + 1 ) ,
	kccZp	= ( kccZl + 1 ) ,
	kccCc	= ( kccZp + 1 ) ,
	kccCf	= ( kccCc + 1 ) ,
	kccCs	= ( kccCf + 1 ) ,
	kccCo	= ( kccCs + 1 ) ,
	kccCn	= ( kccCo + 1 ) ,
	kccPc	= ( kccCn + 1 ) ,
	kccPd	= ( kccPc + 1 ) ,
	kccPs	= ( kccPd + 1 ) ,
	kccPe	= ( kccPs + 1 ) ,
	kccPi	= ( kccPe + 1 ) ,
	kccPf	= ( kccPi + 1 ) ,
	kccPo	= ( kccPf + 1 ) ,
	kccSm	= ( kccPo + 1 ) ,
	kccSc	= ( kccSm + 1 ) ,
	kccSk	= ( kccSc + 1 ) ,
	kccSo	= ( kccSk + 1 )
	} 	LgGeneralCharCategory;

typedef /* [v1_enum] */
enum LgBidiCategory
	{	kbicL	= 0,
	kbicLRE	= ( kbicL + 1 ) ,
	kbicLRO	= ( kbicLRE + 1 ) ,
	kbicR	= ( kbicLRO + 1 ) ,
	kbicAL	= ( kbicR + 1 ) ,
	kbicRLE	= ( kbicAL + 1 ) ,
	kbicRLO	= ( kbicRLE + 1 ) ,
	kbicPDF	= ( kbicRLO + 1 ) ,
	kbicEN	= ( kbicPDF + 1 ) ,
	kbicES	= ( kbicEN + 1 ) ,
	kbicET	= ( kbicES + 1 ) ,
	kbicAN	= ( kbicET + 1 ) ,
	kbicCS	= ( kbicAN + 1 ) ,
	kbicNSM	= ( kbicCS + 1 ) ,
	kbicBN	= ( kbicNSM + 1 ) ,
	kbicB	= ( kbicBN + 1 ) ,
	kbicS	= ( kbicB + 1 ) ,
	kbicWS	= ( kbicS + 1 ) ,
	kbicON	= ( kbicWS + 1 )
	} 	LgBidiCategory;

typedef /* [v1_enum] */
enum LgLBP
	{	klbpAI	= 0,
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
enum LgDecompMapTag
	{	kdtNoTag	= 0,
	kdtFont	= ( kdtNoTag + 1 ) ,
	kdtNoBreak	= ( kdtFont + 1 ) ,
	kdtInitial	= ( kdtNoBreak + 1 ) ,
	kdtMedial	= ( kdtInitial + 1 ) ,
	kdtFinal	= ( kdtMedial + 1 ) ,
	kdtIsolated	= ( kdtFinal + 1 ) ,
	kdtCircle	= ( kdtIsolated + 1 ) ,
	kdtSuper	= ( kdtCircle + 1 ) ,
	kdtSub	= ( kdtSuper + 1 ) ,
	kdtVertical	= ( kdtSub + 1 ) ,
	kdtWide	= ( kdtVertical + 1 ) ,
	kdtNarrow	= ( kdtWide + 1 ) ,
	kdtSmall	= ( kdtNarrow + 1 ) ,
	kdtSquare	= ( kdtSmall + 1 ) ,
	kdtFraction	= ( kdtSquare + 1 ) ,
	kdtCompat	= ( kdtFraction + 1 )
	} 	LgDecompMapTag;

typedef /* [v1_enum] */
enum LgXMLTag
	{	kxmlInvalid	= 0,
	kxmlChardefs	= ( kxmlInvalid + 1 ) ,
	kxmlDef	= ( kxmlChardefs + 1 ) ,
	kxmlUdata	= ( kxmlDef + 1 ) ,
	kxmlLinebrk	= ( kxmlUdata + 1 )
	} 	LgXMLTag;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystem
,
9F74A170-E8BB-466d-8848-5FDB28AC5AF8
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgInputMethodEditor
,
E1B27A5F-DD1B-4BBA-9B72-00BDE03162FC
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgFontManager
,
73718689-B701-4241-A408-4C389ECD6664
);
ATTACH_GUID_TO_CLASS(class,
659C2C2F-7AF6-4F9E-AC6F-7A03C8418FC9
,
LgInputMethodEditor
);
ATTACH_GUID_TO_CLASS(class,
02C3F580-796D-4B5F-BE43-166D97319DA5
,
LgFontManager
);
typedef /* [v1_enum] */
enum LgCollatingOptions
	{	fcoDefault	= 0,
	fcoIgnoreCase	= 1,
	fcoDontIgnoreVariant	= 2,
	fcoLim	= ( fcoDontIgnoreVariant + 1 )
	} 	LgCollatingOptions;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCollatingEngine
,
D27A3D8C-D3FE-4E25-9097-8F4A1FB30361
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCharacterPropertyEngine
,
890C5B18-6E95-438E-8ADE-A4FFADDF0684
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuCharPropEngine
,
E8689492-7622-427b-8518-6339294FD227
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSearchEngine
,
09FCA8D5-5BF6-4BFF-A317-E0126410D79A
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgStringConverter
,
8BE2C911-6A81-48B5-A27F-B8CE63983082
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTokenizer
,
577C6DA1-CFC1-4AFB-82B2-AF818EC2FE9F
);
typedef /* [v1_enum] */
enum LgSpellCheckOptions
	{	fsplcNil	= 0,
	fsplcSuggestFromUserDict	= 0x1,
	fsplcIgnoreAllCaps	= 0x2,
	fsplcIgnoreMixedDigits	= 0x4,
	fsplcIgnoreRomanNumerals	= 0x8,
	fsplcFindUncappedSentences	= 0x10,
	fsplcFindMissingSpaces	= 0x20,
	fsplcFindRepeatWord	= 0x40,
	fsplcFindExtraSpaces	= 0x80,
	fsplcFindSpacesBeforePunc	= 0x100,
	fsplcFindSpacesAfterPunc	= 0x200,
	fsplcFindInitialNumerals	= 0x800,
	fsplcQuickSuggest	= 0x2000,
	fsplcUseAllOpenUdr	= 0x4000,
	fsplcSglStepSugg	= 0x10000,
	fsplcIgnoreSingleLetter	= 0x20000
	} 	LgSpellCheckOptions;

typedef /* [v1_enum] */
enum LgSpellCheckResults
	{	scrsNoErrors	= 0,
	scrsUnknownInputWord	= 1,
	scrsReturningChangeAlways	= 2,
	scrsReturningChangeOnce	= 3,
	scrsInvalidHyphenation	= 4,
	scrsErrorCapitalization	= 5,
	scrsWordConsideredAbbreviation	= 6,
	scrsHyphChangesSpelling	= 7,
	scrsNoMoreSuggestions	= 8,
	scrsMoreInfoThanBufferCouldHold	= 9,
	scrsNoSentenceStartCap	= 10,
	scrsRepeatWord	= 11,
	scrsExtraSpaces	= 12,
	scrsMissingSpace	= 13,
	scrsInitialNumeral	= 14
	} 	LgSpellCheckResults;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSpellChecker
,
E3661AF5-26C6-4907-9243-610DAD84D9D4
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSpellCheckFactory
,
9F9298F5-FD41-44B0-83BA-BED9F56CF974
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgNumericEngine
,
CBBF35E1-CE39-4EEC-AEBD-5B4AAAA52B6C
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystemFactory
,
22376578-BFEB-4c46-8D72-C9154890DD16
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgKeymanHandler
,
3F42144B-509F-4def-8DD3-6D8D26677001
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTextServices
,
5B6303DE-E635-4DD7-A7FC-345BEEF352D8
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCodePageEnumerator
,
2CFCF4B7-2FFE-4CF8-91BE-FBB57CC7782A
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgLanguageEnumerator
,
746A16E1-0C36-4268-A261-E8012B0D67C5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuConverterEnumerator
,
8E6D558E-8755-4EA1-9FF6-039D375312E9
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuTransliteratorEnumerator
,
50F2492C-6C46-48BA-8B7F-5F04153AB2CC
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuLocaleEnumerator
,
08F649D0-D8AB-447B-AAB6-21F85CFA743C
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuResourceBundle
,
C243C72A-0D15-44D9-ABCB-A6E875A7659A
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRegexMatcher
,
6C62CCF0-4EE1-493C-8092-319B6CFBEEBC
);
ATTACH_GUID_TO_CLASS(class,
13D5C6D3-39D9-4BDA-A3F8-A5CAF6A6940A
,
RegexMatcherWrapper
);
ATTACH_GUID_TO_CLASS(class,
E361F805-C902-4306-A5D8-F7802B0E7365
,
LgSystemCollater
);
ATTACH_GUID_TO_CLASS(class,
0D9900D2-1693-481F-AA70-7EA64F264EC4
,
LgUnicodeCollater
);
ATTACH_GUID_TO_CLASS(class,
E771361C-FF54-4120-9525-98A0B7A9ACCF
,
LgIcuCollator
);
ATTACH_GUID_TO_CLASS(class,
30D75676-A10F-48FE-9627-EBF4061EA49D
,
LgIcuCharPropEngine
);
ATTACH_GUID_TO_CLASS(class,
7CE7CE94-AC47-42A5-823F-2F8EF51A9007
,
LgCPWordTokenizer
);
ATTACH_GUID_TO_CLASS(class,
818445E2-0282-4688-8BB7-147FAACFF73A
,
LgWfiSpellChecker
);
ATTACH_GUID_TO_CLASS(class,
5CF96DA5-299E-4FC5-A990-2D2FCEE7834D
,
LgMSWordSpellChecker
);
ATTACH_GUID_TO_CLASS(class,
FF22A7AB-223E-4D04-B648-0AE40588261D
,
LgNumericEngine
);
ATTACH_GUID_TO_CLASS(class,
69ACA99C-F852-4C2C-9B5F-FF83238A17A5
,
LgKeymanHandler
);
ATTACH_GUID_TO_CLASS(class,
720485C5-E8D5-4761-92F0-F70D2B3CF980
,
LgTextServices
);
ATTACH_GUID_TO_CLASS(class,
9045F113-8626-41C0-A61E-A73FBE5920D1
,
LgCodePageEnumerator
);
ATTACH_GUID_TO_CLASS(class,
B887505B-74DE-4ADC-A1D9-5553428C8D02
,
LgLanguageEnumerator
);
ATTACH_GUID_TO_CLASS(class,
9E729461-F80D-4796-BA17-086BC61907F1
,
LgIcuConverterEnumerator
);
ATTACH_GUID_TO_CLASS(class,
3F1FD0A4-B2B1-4589-BC82-9CEF5BA84F4E
,
LgIcuTransliteratorEnumerator
);
ATTACH_GUID_TO_CLASS(class,
0DD7FC1A-AB97-4A39-882C-269760D86619
,
LgIcuResourceBundle
);
ATTACH_GUID_TO_CLASS(class,
E426656C-64F7-480E-92F4-D41A7BFFD066
,
LgIcuLocaleEnumerator
);

#define LIBID_FwKernelLib __uuidof(FwKernelLib)

#ifndef __ITsString_INTERFACE_DEFINED__
#define __ITsString_INTERFACE_DEFINED__

/* interface ITsString */
/* [unique][object][uuid] */


#define IID_ITsString __uuidof(ITsString)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("295B2E11-B149-49C5-9BE9-9F46185609AA")
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

		virtual HRESULT STDMETHODCALLTYPE GetFactoryClsid(
			/* [retval][out] */ CLSID *pclsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmt(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmtRgb(
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded) = 0;

		virtual HRESULT STDMETHODCALLTYPE Equals(
			/* [in] */ ITsString *ptss,
			/* [retval][out] */ ComBool *pfEqual) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteAsXml(
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetXmlString(
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsNormalizedForm(
			/* [in] */ FwNormalizationMode nm,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NormalizedForm(
			/* [in] */ FwNormalizationMode nm,
			/* [retval][out] */ ITsString **pptssRet) = 0;

		virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE NfdAndFixOffsets(
			/* [out] */ ITsString **pptssRet,
			/* [size_is][in] */ int **prgpichOffsetsToFix,
			/* [in] */ int cichOffsetsToFix) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetSubstring(
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [retval][out] */ ITsString **pptssRet) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteAsXmlExtended(
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData,
			/* [in] */ ComBool fUseRFC4646) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_StringProperty(
			/* [in] */ int iRun,
			/* [in] */ int tpt,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_StringPropertyAt(
			/* [in] */ int ich,
			/* [in] */ int tpt,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_WritingSystem(
			/* [in] */ int irun,
			/* [retval][out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_WritingSystemAt(
			/* [in] */ int ich,
			/* [retval][out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_IsRunOrc(
			/* [in] */ int iRun,
			/* [retval][out] */ ComBool *pfIsOrc) = 0;

	};

#else 	/* C style interface */

	typedef struct ITsStringVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsString * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *GetFactoryClsid )(
			ITsString * This,
			/* [retval][out] */ CLSID *pclsid);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmt )(
			ITsString * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmtRgb )(
			ITsString * This,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded);

		HRESULT ( STDMETHODCALLTYPE *Equals )(
			ITsString * This,
			/* [in] */ ITsString *ptss,
			/* [retval][out] */ ComBool *pfEqual);

		HRESULT ( STDMETHODCALLTYPE *WriteAsXml )(
			ITsString * This,
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData);

		HRESULT ( STDMETHODCALLTYPE *GetXmlString )(
			ITsString * This,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData,
			/* [retval][out] */ BSTR *pbstr);

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
			/* [size_is][in] */ int **prgpichOffsetsToFix,
			/* [in] */ int cichOffsetsToFix);

		HRESULT ( STDMETHODCALLTYPE *GetSubstring )(
			ITsString * This,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [retval][out] */ ITsString **pptssRet);

		HRESULT ( STDMETHODCALLTYPE *WriteAsXmlExtended )(
			ITsString * This,
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData,
			/* [in] */ ComBool fUseRFC4646);

		HRESULT ( STDMETHODCALLTYPE *get_StringProperty )(
			ITsString * This,
			/* [in] */ int iRun,
			/* [in] */ int tpt,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *get_StringPropertyAt )(
			ITsString * This,
			/* [in] */ int ich,
			/* [in] */ int tpt,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *get_WritingSystem )(
			ITsString * This,
			/* [in] */ int irun,
			/* [retval][out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *get_WritingSystemAt )(
			ITsString * This,
			/* [in] */ int ich,
			/* [retval][out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *get_IsRunOrc )(
			ITsString * This,
			/* [in] */ int iRun,
			/* [retval][out] */ ComBool *pfIsOrc);

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

#define ITsString_GetFactoryClsid(This,pclsid)	\
	( (This)->lpVtbl -> GetFactoryClsid(This,pclsid) )

#define ITsString_SerializeFmt(This,pstrm)	\
	( (This)->lpVtbl -> SerializeFmt(This,pstrm) )

#define ITsString_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	( (This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded) )

#define ITsString_Equals(This,ptss,pfEqual)	\
	( (This)->lpVtbl -> Equals(This,ptss,pfEqual) )

#define ITsString_WriteAsXml(This,pstrm,pwsf,cchIndent,ws,fWriteObjData)	\
	( (This)->lpVtbl -> WriteAsXml(This,pstrm,pwsf,cchIndent,ws,fWriteObjData) )

#define ITsString_GetXmlString(This,pwsf,cchIndent,ws,fWriteObjData,pbstr)	\
	( (This)->lpVtbl -> GetXmlString(This,pwsf,cchIndent,ws,fWriteObjData,pbstr) )

#define ITsString_get_IsNormalizedForm(This,nm,pfRet)	\
	( (This)->lpVtbl -> get_IsNormalizedForm(This,nm,pfRet) )

#define ITsString_get_NormalizedForm(This,nm,pptssRet)	\
	( (This)->lpVtbl -> get_NormalizedForm(This,nm,pptssRet) )

#define ITsString_NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix)	\
	( (This)->lpVtbl -> NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix) )

#define ITsString_GetSubstring(This,ichMin,ichLim,pptssRet)	\
	( (This)->lpVtbl -> GetSubstring(This,ichMin,ichLim,pptssRet) )

#define ITsString_WriteAsXmlExtended(This,pstrm,pwsf,cchIndent,ws,fWriteObjData,fUseRFC4646)	\
	( (This)->lpVtbl -> WriteAsXmlExtended(This,pstrm,pwsf,cchIndent,ws,fWriteObjData,fUseRFC4646) )

#define ITsString_get_StringProperty(This,iRun,tpt,pbstr)	\
	( (This)->lpVtbl -> get_StringProperty(This,iRun,tpt,pbstr) )

#define ITsString_get_StringPropertyAt(This,ich,tpt,pbstr)	\
	( (This)->lpVtbl -> get_StringPropertyAt(This,ich,tpt,pbstr) )

#define ITsString_get_WritingSystem(This,irun,pws)	\
	( (This)->lpVtbl -> get_WritingSystem(This,irun,pws) )

#define ITsString_get_WritingSystemAt(This,ich,pws)	\
	( (This)->lpVtbl -> get_WritingSystemAt(This,ich,pws) )

#define ITsString_get_IsRunOrc(This,iRun,pfIsOrc)	\
	( (This)->lpVtbl -> get_IsRunOrc(This,iRun,pfIsOrc) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsString_INTERFACE_DEFINED__ */


#ifndef __IUndoGrouper_INTERFACE_DEFINED__
#define __IUndoGrouper_INTERFACE_DEFINED__

/* interface IUndoGrouper */
/* [unique][object][uuid] */


#define IID_IUndoGrouper __uuidof(IUndoGrouper)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("C38348D3-392C-4e02-BD50-A01DC4189E1D")
	IUndoGrouper : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE BeginGroup(
			/* [retval][out] */ int *phndl) = 0;

		virtual HRESULT STDMETHODCALLTYPE EndGroup(
			/* [in] */ int hndl) = 0;

		virtual HRESULT STDMETHODCALLTYPE CancelGroup(
			/* [in] */ int hndl) = 0;

	};

#else 	/* C style interface */

	typedef struct IUndoGrouperVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IUndoGrouper * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IUndoGrouper * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IUndoGrouper * This);

		HRESULT ( STDMETHODCALLTYPE *BeginGroup )(
			IUndoGrouper * This,
			/* [retval][out] */ int *phndl);

		HRESULT ( STDMETHODCALLTYPE *EndGroup )(
			IUndoGrouper * This,
			/* [in] */ int hndl);

		HRESULT ( STDMETHODCALLTYPE *CancelGroup )(
			IUndoGrouper * This,
			/* [in] */ int hndl);

		END_INTERFACE
	} IUndoGrouperVtbl;

	interface IUndoGrouper
	{
		CONST_VTBL struct IUndoGrouperVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IUndoGrouper_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define IUndoGrouper_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define IUndoGrouper_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define IUndoGrouper_BeginGroup(This,phndl)	\
	( (This)->lpVtbl -> BeginGroup(This,phndl) )

#define IUndoGrouper_EndGroup(This,hndl)	\
	( (This)->lpVtbl -> EndGroup(This,hndl) )

#define IUndoGrouper_CancelGroup(This,hndl)	\
	( (This)->lpVtbl -> CancelGroup(This,hndl) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IUndoGrouper_INTERFACE_DEFINED__ */


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
			__RPC__deref_out  void **ppvObject);

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
			__RPC__deref_out  void **ppvObject);

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

	MIDL_INTERFACE("0F8EA3BE-C982-40f8-B674-25B8482EB222")
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

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoGrouper(
			/* [retval][out] */ IUndoGrouper **ppundg) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UndoGrouper(
			/* [in] */ IUndoGrouper *pundg) = 0;

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
			__RPC__deref_out  void **ppvObject);

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

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UndoGrouper )(
			IActionHandler * This,
			/* [retval][out] */ IUndoGrouper **ppundg);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UndoGrouper )(
			IActionHandler * This,
			/* [in] */ IUndoGrouper *pundg);

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

#define IActionHandler_get_UndoGrouper(This,ppundg)	\
	( (This)->lpVtbl -> get_UndoGrouper(This,ppundg) )

#define IActionHandler_put_UndoGrouper(This,pundg)	\
	( (This)->lpVtbl -> put_UndoGrouper(This,pundg) )

#define IActionHandler_get_IsUndoOrRedoInProgress(This,pfInProgress)	\
	( (This)->lpVtbl -> get_IsUndoOrRedoInProgress(This,pfInProgress) )

#define IActionHandler_get_SuppressSelections(This,pfSupressSel)	\
	( (This)->lpVtbl -> get_SuppressSelections(This,pfSupressSel) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IActionHandler_INTERFACE_DEFINED__ */


#define CLSID_ActionHandler __uuidof(ActionHandler)

#ifdef __cplusplus

class DECLSPEC_UUID("6A46D810-7F14-4151-80F5-0B13FFC1F917")
ActionHandler;
#endif

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
			__RPC__deref_out  void **ppvObject);

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
			__RPC__deref_out  void **ppvObject);

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


#define CLSID_DebugReport __uuidof(DebugReport)

#ifdef __cplusplus

class DECLSPEC_UUID("24636FD1-DB8D-4b2c-B4C0-44C2592CA482")
DebugReport;
#endif

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
			__RPC__deref_out  void **ppvObject);

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


#ifndef __ITsTextProps_INTERFACE_DEFINED__
#define __ITsTextProps_INTERFACE_DEFINED__

/* interface ITsTextProps */
/* [unique][object][uuid] */


#define IID_ITsTextProps __uuidof(ITsTextProps)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("4FA0B99A-5A56-41A4-BE8B-B89BC62251A5")
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

		virtual HRESULT STDMETHODCALLTYPE GetFactoryClsid(
			/* [retval][out] */ CLSID *pclsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeRgb(
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcb) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeRgPropsRgb(
			/* [in] */ int cpttp,
			/* [in] */ ITsTextProps **rgpttp,
			/* [in] */ int *rgich,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcb) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteAsXml(
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent) = 0;

	};

#else 	/* C style interface */

	typedef struct ITsTextPropsVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsTextProps * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *GetFactoryClsid )(
			ITsTextProps * This,
			/* [retval][out] */ CLSID *pclsid);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			ITsTextProps * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *SerializeRgb )(
			ITsTextProps * This,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcb);

		HRESULT ( STDMETHODCALLTYPE *SerializeRgPropsRgb )(
			ITsTextProps * This,
			/* [in] */ int cpttp,
			/* [in] */ ITsTextProps **rgpttp,
			/* [in] */ int *rgich,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcb);

		HRESULT ( STDMETHODCALLTYPE *WriteAsXml )(
			ITsTextProps * This,
			/* [in] */ IStream *pstrm,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent);

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

#define ITsTextProps_GetFactoryClsid(This,pclsid)	\
	( (This)->lpVtbl -> GetFactoryClsid(This,pclsid) )

#define ITsTextProps_Serialize(This,pstrm)	\
	( (This)->lpVtbl -> Serialize(This,pstrm) )

#define ITsTextProps_SerializeRgb(This,prgb,cbMax,pcb)	\
	( (This)->lpVtbl -> SerializeRgb(This,prgb,cbMax,pcb) )

#define ITsTextProps_SerializeRgPropsRgb(This,cpttp,rgpttp,rgich,prgb,cbMax,pcb)	\
	( (This)->lpVtbl -> SerializeRgPropsRgb(This,cpttp,rgpttp,rgich,prgb,cbMax,pcb) )

#define ITsTextProps_WriteAsXml(This,pstrm,pwsf,cchIndent)	\
	( (This)->lpVtbl -> WriteAsXml(This,pstrm,pwsf,cchIndent) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsTextProps_INTERFACE_DEFINED__ */


#ifndef __ITsStrFactory_INTERFACE_DEFINED__
#define __ITsStrFactory_INTERFACE_DEFINED__

/* interface ITsStrFactory */
/* [unique][object][uuid] */


#define IID_ITsStrFactory __uuidof(ITsStrFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("C10EA417-8317-4048-AC90-103F8BDFB325")
	ITsStrFactory : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE DeserializeStringStreams(
			/* [in] */ IStream *pstrmTxt,
			/* [in] */ IStream *pstrmFmt,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeserializeString(
			/* [in] */ BSTR bstrTxt,
			/* [in] */ IStream *pstrmFmt,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeserializeStringRgb(
			/* [in] */ BSTR bstrTxt,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeserializeStringRgch(
			/* [size_is][in] */ const OLECHAR *prgchTxt,
			/* [out][in] */ int *pcchTxt,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [out][in] */ int *pcbFmt,
			/* [retval][out] */ ITsString **pptss) = 0;

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

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunCount(
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [retval][out] */ int *pcrun) = 0;

		virtual HRESULT STDMETHODCALLTYPE FetchRunInfoAt(
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [in] */ int ich,
			/* [out] */ TsRunInfo *ptri,
			/* [retval][out] */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE FetchRunInfo(
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [in] */ int irun,
			/* [out] */ TsRunInfo *ptri,
			/* [retval][out] */ ITsTextProps **ppttp) = 0;

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
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ITsStrFactory * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ITsStrFactory * This);

		HRESULT ( STDMETHODCALLTYPE *DeserializeStringStreams )(
			ITsStrFactory * This,
			/* [in] */ IStream *pstrmTxt,
			/* [in] */ IStream *pstrmFmt,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *DeserializeString )(
			ITsStrFactory * This,
			/* [in] */ BSTR bstrTxt,
			/* [in] */ IStream *pstrmFmt,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *DeserializeStringRgb )(
			ITsStrFactory * This,
			/* [in] */ BSTR bstrTxt,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *DeserializeStringRgch )(
			ITsStrFactory * This,
			/* [size_is][in] */ const OLECHAR *prgchTxt,
			/* [out][in] */ int *pcchTxt,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [out][in] */ int *pcbFmt,
			/* [retval][out] */ ITsString **pptss);

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

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunCount )(
			ITsStrFactory * This,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [retval][out] */ int *pcrun);

		HRESULT ( STDMETHODCALLTYPE *FetchRunInfoAt )(
			ITsStrFactory * This,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [in] */ int ich,
			/* [out] */ TsRunInfo *ptri,
			/* [retval][out] */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *FetchRunInfo )(
			ITsStrFactory * This,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [in] */ int cbFmt,
			/* [in] */ int irun,
			/* [out] */ TsRunInfo *ptri,
			/* [retval][out] */ ITsTextProps **ppttp);

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


#define ITsStrFactory_DeserializeStringStreams(This,pstrmTxt,pstrmFmt,pptss)	\
	( (This)->lpVtbl -> DeserializeStringStreams(This,pstrmTxt,pstrmFmt,pptss) )

#define ITsStrFactory_DeserializeString(This,bstrTxt,pstrmFmt,pptss)	\
	( (This)->lpVtbl -> DeserializeString(This,bstrTxt,pstrmFmt,pptss) )

#define ITsStrFactory_DeserializeStringRgb(This,bstrTxt,prgbFmt,cbFmt,pptss)	\
	( (This)->lpVtbl -> DeserializeStringRgb(This,bstrTxt,prgbFmt,cbFmt,pptss) )

#define ITsStrFactory_DeserializeStringRgch(This,prgchTxt,pcchTxt,prgbFmt,pcbFmt,pptss)	\
	( (This)->lpVtbl -> DeserializeStringRgch(This,prgchTxt,pcchTxt,prgbFmt,pcbFmt,pptss) )

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

#define ITsStrFactory_get_RunCount(This,prgbFmt,cbFmt,pcrun)	\
	( (This)->lpVtbl -> get_RunCount(This,prgbFmt,cbFmt,pcrun) )

#define ITsStrFactory_FetchRunInfoAt(This,prgbFmt,cbFmt,ich,ptri,ppttp)	\
	( (This)->lpVtbl -> FetchRunInfoAt(This,prgbFmt,cbFmt,ich,ptri,ppttp) )

#define ITsStrFactory_FetchRunInfo(This,prgbFmt,cbFmt,irun,ptri,ppttp)	\
	( (This)->lpVtbl -> FetchRunInfo(This,prgbFmt,cbFmt,irun,ptri,ppttp) )

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

	MIDL_INTERFACE("8DCE56A6-CFF1-4402-95FE-2B574912B54E")
	ITsPropsFactory : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE DeserializeProps(
			/* [in] */ IStream *pstrm,
			/* [retval][out] */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeserializePropsRgb(
			/* [size_is][in] */ const BYTE *prgb,
			/* [out][in] */ int *pcb,
			/* [retval][out] */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeserializeRgPropsRgb(
			/* [in] */ int cpttpMax,
			/* [size_is][in] */ const BYTE *prgb,
			/* [out][in] */ int *pcb,
			/* [out] */ int *pcpttpRet,
			/* [size_is][out] */ ITsTextProps **rgpttp,
			/* [size_is][out] */ int *rgich) = 0;

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
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ITsPropsFactory * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ITsPropsFactory * This);

		HRESULT ( STDMETHODCALLTYPE *DeserializeProps )(
			ITsPropsFactory * This,
			/* [in] */ IStream *pstrm,
			/* [retval][out] */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *DeserializePropsRgb )(
			ITsPropsFactory * This,
			/* [size_is][in] */ const BYTE *prgb,
			/* [out][in] */ int *pcb,
			/* [retval][out] */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *DeserializeRgPropsRgb )(
			ITsPropsFactory * This,
			/* [in] */ int cpttpMax,
			/* [size_is][in] */ const BYTE *prgb,
			/* [out][in] */ int *pcb,
			/* [out] */ int *pcpttpRet,
			/* [size_is][out] */ ITsTextProps **rgpttp,
			/* [size_is][out] */ int *rgich);

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


#define ITsPropsFactory_DeserializeProps(This,pstrm,ppttp)	\
	( (This)->lpVtbl -> DeserializeProps(This,pstrm,ppttp) )

#define ITsPropsFactory_DeserializePropsRgb(This,prgb,pcb,ppttp)	\
	( (This)->lpVtbl -> DeserializePropsRgb(This,prgb,pcb,ppttp) )

#define ITsPropsFactory_DeserializeRgPropsRgb(This,cpttpMax,prgb,pcb,pcpttpRet,rgpttp,rgich)	\
	( (This)->lpVtbl -> DeserializeRgPropsRgb(This,cpttpMax,prgb,pcb,pcpttpRet,rgpttp,rgich) )

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

	MIDL_INTERFACE("F1EF76E6-BE04-11d3-8D9A-005004DEFEC4")
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
			/* [size_is][in] */ const OLECHAR *prgchIns,
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

		virtual HRESULT STDMETHODCALLTYPE GetBldrClsid(
			/* [retval][out] */ CLSID *pclsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmt(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmtRgb(
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded) = 0;

	};

#else 	/* C style interface */

	typedef struct ITsStrBldrVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsStrBldr * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

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
			/* [size_is][in] */ const OLECHAR *prgchIns,
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

		HRESULT ( STDMETHODCALLTYPE *GetBldrClsid )(
			ITsStrBldr * This,
			/* [retval][out] */ CLSID *pclsid);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmt )(
			ITsStrBldr * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmtRgb )(
			ITsStrBldr * This,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded);

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

#define ITsStrBldr_GetBldrClsid(This,pclsid)	\
	( (This)->lpVtbl -> GetBldrClsid(This,pclsid) )

#define ITsStrBldr_SerializeFmt(This,pstrm)	\
	( (This)->lpVtbl -> SerializeFmt(This,pstrm) )

#define ITsStrBldr_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	( (This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsStrBldr_INTERFACE_DEFINED__ */


#ifndef __ITsIncStrBldr_INTERFACE_DEFINED__
#define __ITsIncStrBldr_INTERFACE_DEFINED__

/* interface ITsIncStrBldr */
/* [unique][object][uuid] */


#define IID_ITsIncStrBldr __uuidof(ITsIncStrBldr)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F1EF76E7-BE04-11d3-8D9A-005004DEFEC4")
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

		virtual HRESULT STDMETHODCALLTYPE GetIncBldrClsid(
			/* [retval][out] */ CLSID *pclsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmt(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE SerializeFmtRgb(
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded) = 0;

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
			__RPC__deref_out  void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *GetIncBldrClsid )(
			ITsIncStrBldr * This,
			/* [retval][out] */ CLSID *pclsid);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmt )(
			ITsIncStrBldr * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *SerializeFmtRgb )(
			ITsIncStrBldr * This,
			/* [size_is][out] */ BYTE *prgb,
			/* [in] */ int cbMax,
			/* [retval][out] */ int *pcbNeeded);

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

#define ITsIncStrBldr_GetIncBldrClsid(This,pclsid)	\
	( (This)->lpVtbl -> GetIncBldrClsid(This,pclsid) )

#define ITsIncStrBldr_SerializeFmt(This,pstrm)	\
	( (This)->lpVtbl -> SerializeFmt(This,pstrm) )

#define ITsIncStrBldr_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	( (This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded) )

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
			/* [out] */ int *pnVal) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetIntPropValues(
			/* [in] */ int tpt,
			/* [out] */ int *pnVar,
			/* [out] */ int *pnVal) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StrPropCount(
			/* [retval][out] */ int *pcv) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetStrProp(
			/* [in] */ int iv,
			/* [out] */ int *ptpt,
			/* [out] */ BSTR *pbstrVal) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetStrPropValue(
			/* [in] */ int tpt,
			/* [out] */ BSTR *pbstrVal) = 0;

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
			__RPC__deref_out  void **ppvObject);

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
			/* [out] */ int *pnVal);

		HRESULT ( STDMETHODCALLTYPE *GetIntPropValues )(
			ITsPropsBldr * This,
			/* [in] */ int tpt,
			/* [out] */ int *pnVar,
			/* [out] */ int *pnVal);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StrPropCount )(
			ITsPropsBldr * This,
			/* [retval][out] */ int *pcv);

		HRESULT ( STDMETHODCALLTYPE *GetStrProp )(
			ITsPropsBldr * This,
			/* [in] */ int iv,
			/* [out] */ int *ptpt,
			/* [out] */ BSTR *pbstrVal);

		HRESULT ( STDMETHODCALLTYPE *GetStrPropValue )(
			ITsPropsBldr * This,
			/* [in] */ int tpt,
			/* [out] */ BSTR *pbstrVal);

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
			__RPC__deref_out  void **ppvObject);

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


#ifndef __ILgWritingSystemFactory_INTERFACE_DEFINED__
#define __ILgWritingSystemFactory_INTERFACE_DEFINED__

/* interface ILgWritingSystemFactory */
/* [unique][object][uuid] */


#define IID_ILgWritingSystemFactory __uuidof(ILgWritingSystemFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("22376578-BFEB-4c46-8D72-C9154890DD16")
	ILgWritingSystemFactory : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Engine(
			/* [in] */ BSTR bstrIcuLocale,
			/* [retval][out] */ ILgWritingSystem **ppwseng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EngineOrNull(
			/* [in] */ int ws,
			/* [retval][out] */ ILgWritingSystem **ppwseng) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetWsFromStr(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ int *pwsId) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetStrFromWs(
			/* [in] */ int wsId,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NumberOfWs(
			/* [retval][out] */ int *pcws) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetWritingSystems(
			/* [size_is][out] */ int *rgws,
			/* [in] */ int cws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CharPropEngine(
			/* [in] */ int ws,
			/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Renderer(
			/* [in] */ int ws,
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppre) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RendererFromChrp(
			/* [in] */ IVwGraphics *pvg,
			/* [out][in] */ LgCharRenderProps *pchrp,
			/* [retval][out] */ IRenderEngine **ppre) = 0;

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
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgWritingSystemFactory * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgWritingSystemFactory * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Engine )(
			ILgWritingSystemFactory * This,
			/* [in] */ BSTR bstrIcuLocale,
			/* [retval][out] */ ILgWritingSystem **ppwseng);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EngineOrNull )(
			ILgWritingSystemFactory * This,
			/* [in] */ int ws,
			/* [retval][out] */ ILgWritingSystem **ppwseng);

		HRESULT ( STDMETHODCALLTYPE *GetWsFromStr )(
			ILgWritingSystemFactory * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ int *pwsId);

		HRESULT ( STDMETHODCALLTYPE *GetStrFromWs )(
			ILgWritingSystemFactory * This,
			/* [in] */ int wsId,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NumberOfWs )(
			ILgWritingSystemFactory * This,
			/* [retval][out] */ int *pcws);

		HRESULT ( STDMETHODCALLTYPE *GetWritingSystems )(
			ILgWritingSystemFactory * This,
			/* [size_is][out] */ int *rgws,
			/* [in] */ int cws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CharPropEngine )(
			ILgWritingSystemFactory * This,
			/* [in] */ int ws,
			/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Renderer )(
			ILgWritingSystemFactory * This,
			/* [in] */ int ws,
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppre);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RendererFromChrp )(
			ILgWritingSystemFactory * This,
			/* [in] */ IVwGraphics *pvg,
			/* [out][in] */ LgCharRenderProps *pchrp,
			/* [retval][out] */ IRenderEngine **ppre);

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


#define ILgWritingSystemFactory_get_Engine(This,bstrIcuLocale,ppwseng)	\
	( (This)->lpVtbl -> get_Engine(This,bstrIcuLocale,ppwseng) )

#define ILgWritingSystemFactory_get_EngineOrNull(This,ws,ppwseng)	\
	( (This)->lpVtbl -> get_EngineOrNull(This,ws,ppwseng) )

#define ILgWritingSystemFactory_GetWsFromStr(This,bstr,pwsId)	\
	( (This)->lpVtbl -> GetWsFromStr(This,bstr,pwsId) )

#define ILgWritingSystemFactory_GetStrFromWs(This,wsId,pbstr)	\
	( (This)->lpVtbl -> GetStrFromWs(This,wsId,pbstr) )

#define ILgWritingSystemFactory_get_NumberOfWs(This,pcws)	\
	( (This)->lpVtbl -> get_NumberOfWs(This,pcws) )

#define ILgWritingSystemFactory_GetWritingSystems(This,rgws,cws)	\
	( (This)->lpVtbl -> GetWritingSystems(This,rgws,cws) )

#define ILgWritingSystemFactory_get_CharPropEngine(This,ws,pplcpe)	\
	( (This)->lpVtbl -> get_CharPropEngine(This,ws,pplcpe) )

#define ILgWritingSystemFactory_get_Renderer(This,ws,pvg,ppre)	\
	( (This)->lpVtbl -> get_Renderer(This,ws,pvg,ppre) )

#define ILgWritingSystemFactory_get_RendererFromChrp(This,pvg,pchrp,ppre)	\
	( (This)->lpVtbl -> get_RendererFromChrp(This,pvg,pchrp,ppre) )

#define ILgWritingSystemFactory_get_UserWs(This,pws)	\
	( (This)->lpVtbl -> get_UserWs(This,pws) )

#define ILgWritingSystemFactory_put_UserWs(This,ws)	\
	( (This)->lpVtbl -> put_UserWs(This,ws) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgWritingSystemFactory_INTERFACE_DEFINED__ */


#define CLSID_TsStrFactory __uuidof(TsStrFactory)

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76E9-BE04-11d3-8D9A-005004DEFEC4")
TsStrFactory;
#endif

#define CLSID_TsPropsFactory __uuidof(TsPropsFactory)

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EA-BE04-11d3-8D9A-005004DEFEC4")
TsPropsFactory;
#endif

#define CLSID_TsStrBldr __uuidof(TsStrBldr)

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EB-BE04-11d3-8D9A-005004DEFEC4")
TsStrBldr;
#endif

#define CLSID_TsIncStrBldr __uuidof(TsIncStrBldr)

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EC-BE04-11d3-8D9A-005004DEFEC4")
TsIncStrBldr;
#endif

#define CLSID_TsPropsBldr __uuidof(TsPropsBldr)

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76ED-BE04-11d3-8D9A-005004DEFEC4")
TsPropsBldr;
#endif

#define CLSID_TsMultiString __uuidof(TsMultiString)

#ifdef __cplusplus

class DECLSPEC_UUID("7A1B89C0-C2D6-11d3-9BB7-00400541F9E9")
TsMultiString;
#endif

#ifndef __ILgInputMethodEditor_INTERFACE_DEFINED__
#define __ILgInputMethodEditor_INTERFACE_DEFINED__

/* interface ILgInputMethodEditor */
/* [unique][object][uuid] */


#define IID_ILgInputMethodEditor __uuidof(ILgInputMethodEditor)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("E1B27A5F-DD1B-4BBA-9B72-00BDE03162FC")
	ILgInputMethodEditor : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Setup( void) = 0;

		virtual /* [restricted][local] */ HRESULT STDMETHODCALLTYPE Replace(
			/* [in] */ BSTR bstrInput,
			/* [in] */ ITsTextProps *pttpInput,
			/* [in] */ ITsStrBldr *ptsbOld,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP) = 0;

		virtual HRESULT STDMETHODCALLTYPE Backspace(
			/* [in] */ int pichStart,
			/* [in] */ int cactBackspace,
			/* [in] */ ITsStrBldr *ptsbOld,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP,
			/* [out] */ int *pcactBsRemaining) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeleteForward(
			/* [in] */ int pichStart,
			/* [in] */ int cactDelForward,
			/* [in] */ ITsStrBldr *ptsbInOut,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP,
			/* [out] */ int *pcactDfRemaining) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsValidInsertionPoint(
			/* [in] */ int ich,
			/* [in] */ ITsString *ptss,
			/* [retval][out] */ BOOL *pfValid) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgInputMethodEditorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgInputMethodEditor * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgInputMethodEditor * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgInputMethodEditor * This);

		HRESULT ( STDMETHODCALLTYPE *Setup )(
			ILgInputMethodEditor * This);

		/* [restricted][local] */ HRESULT ( STDMETHODCALLTYPE *Replace )(
			ILgInputMethodEditor * This,
			/* [in] */ BSTR bstrInput,
			/* [in] */ ITsTextProps *pttpInput,
			/* [in] */ ITsStrBldr *ptsbOld,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP);

		HRESULT ( STDMETHODCALLTYPE *Backspace )(
			ILgInputMethodEditor * This,
			/* [in] */ int pichStart,
			/* [in] */ int cactBackspace,
			/* [in] */ ITsStrBldr *ptsbOld,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP,
			/* [out] */ int *pcactBsRemaining);

		HRESULT ( STDMETHODCALLTYPE *DeleteForward )(
			ILgInputMethodEditor * This,
			/* [in] */ int pichStart,
			/* [in] */ int cactDelForward,
			/* [in] */ ITsStrBldr *ptsbInOut,
			/* [out] */ int *pichModMin,
			/* [out] */ int *pichModLim,
			/* [out] */ int *pichIP,
			/* [out] */ int *pcactDfRemaining);

		HRESULT ( STDMETHODCALLTYPE *IsValidInsertionPoint )(
			ILgInputMethodEditor * This,
			/* [in] */ int ich,
			/* [in] */ ITsString *ptss,
			/* [retval][out] */ BOOL *pfValid);

		END_INTERFACE
	} ILgInputMethodEditorVtbl;

	interface ILgInputMethodEditor
	{
		CONST_VTBL struct ILgInputMethodEditorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgInputMethodEditor_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgInputMethodEditor_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgInputMethodEditor_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgInputMethodEditor_Setup(This)	\
	( (This)->lpVtbl -> Setup(This) )

#define ILgInputMethodEditor_Replace(This,bstrInput,pttpInput,ptsbOld,ichMin,ichLim,pichModMin,pichModLim,pichIP)	\
	( (This)->lpVtbl -> Replace(This,bstrInput,pttpInput,ptsbOld,ichMin,ichLim,pichModMin,pichModLim,pichIP) )

#define ILgInputMethodEditor_Backspace(This,pichStart,cactBackspace,ptsbOld,pichModMin,pichModLim,pichIP,pcactBsRemaining)	\
	( (This)->lpVtbl -> Backspace(This,pichStart,cactBackspace,ptsbOld,pichModMin,pichModLim,pichIP,pcactBsRemaining) )

#define ILgInputMethodEditor_DeleteForward(This,pichStart,cactDelForward,ptsbInOut,pichModMin,pichModLim,pichIP,pcactDfRemaining)	\
	( (This)->lpVtbl -> DeleteForward(This,pichStart,cactDelForward,ptsbInOut,pichModMin,pichModLim,pichIP,pcactDfRemaining) )

#define ILgInputMethodEditor_IsValidInsertionPoint(This,ich,ptss,pfValid)	\
	( (This)->lpVtbl -> IsValidInsertionPoint(This,ich,ptss,pfValid) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgInputMethodEditor_INTERFACE_DEFINED__ */


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

		virtual HRESULT STDMETHODCALLTYPE DrawTextExt(
			/* [in] */ int x,
			/* [in] */ int y,
			/* [in] */ int cch,
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ UINT uOptions,
			/* [in] */ const RECT *prect,
			/* [in] */ int *prgdx) = 0;

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
			/* [out] */ int *pcbTableSz,
			/* [retval][out] */ BSTR *pbstrTableData) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFontDataRgch(
			/* [in] */ int nTableId,
			/* [out] */ int *pcbTableSz,
			/* [size_is][out] */ OLECHAR *prgch,
			/* [in] */ int cchMax) = 0;

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
			/* [retval][out] */ LgCharRenderProps *pchrp) = 0;

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
			/* [out][in] */ LgCharRenderProps *pchrp) = 0;

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
			__RPC__deref_out  void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *DrawTextExt )(
			IVwGraphics * This,
			/* [in] */ int x,
			/* [in] */ int y,
			/* [in] */ int cch,
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ UINT uOptions,
			/* [in] */ const RECT *prect,
			/* [in] */ int *prgdx);

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
			/* [out] */ int *pcbTableSz,
			/* [retval][out] */ BSTR *pbstrTableData);

		HRESULT ( STDMETHODCALLTYPE *GetFontDataRgch )(
			IVwGraphics * This,
			/* [in] */ int nTableId,
			/* [out] */ int *pcbTableSz,
			/* [size_is][out] */ OLECHAR *prgch,
			/* [in] */ int cchMax);

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
			/* [retval][out] */ LgCharRenderProps *pchrp);

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
			/* [out][in] */ LgCharRenderProps *pchrp);

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

#define IVwGraphics_DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)	\
	( (This)->lpVtbl -> DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx) )

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

#define IVwGraphics_GetFontData(This,nTableId,pcbTableSz,pbstrTableData)	\
	( (This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,pbstrTableData) )

#define IVwGraphics_GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)	\
	( (This)->lpVtbl -> GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax) )

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
			__RPC__deref_out  void **ppvObject);

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
			__RPC__deref_out  void **ppvObject);

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
			__RPC__deref_out  void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *DrawTextExt )(
			IVwGraphicsWin32 * This,
			/* [in] */ int x,
			/* [in] */ int y,
			/* [in] */ int cch,
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ UINT uOptions,
			/* [in] */ const RECT *prect,
			/* [in] */ int *prgdx);

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
			/* [out] */ int *pcbTableSz,
			/* [retval][out] */ BSTR *pbstrTableData);

		HRESULT ( STDMETHODCALLTYPE *GetFontDataRgch )(
			IVwGraphicsWin32 * This,
			/* [in] */ int nTableId,
			/* [out] */ int *pcbTableSz,
			/* [size_is][out] */ OLECHAR *prgch,
			/* [in] */ int cchMax);

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
			/* [retval][out] */ LgCharRenderProps *pchrp);

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
			/* [out][in] */ LgCharRenderProps *pchrp);

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

#define IVwGraphicsWin32_DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)	\
	( (This)->lpVtbl -> DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx) )

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

#define IVwGraphicsWin32_GetFontData(This,nTableId,pcbTableSz,pbstrTableData)	\
	( (This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,pbstrTableData) )

#define IVwGraphicsWin32_GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)	\
	( (This)->lpVtbl -> GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax) )

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
			/* [out] */ LgCharRenderProps *pchrp,
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
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetWsFactory(
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

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
			__RPC__deref_out  void **ppvObject);

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
			/* [out] */ LgCharRenderProps *pchrp,
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
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *GetWsFactory )(
			IVwTextSource * This,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

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
			__RPC__deref_out  void **ppvObject);

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


#define CLSID_VwJustifier __uuidof(VwJustifier)

#ifdef __cplusplus

class DECLSPEC_UUID("D3E3ADB7-94CB-443B-BB8F-82A03BF850F3")
VwJustifier;
#endif

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
			__RPC__deref_out  void **ppvObject);

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

	MIDL_INTERFACE("7F4B8F79-2A40-408C-944B-848B14D65D23")
	IRenderEngine : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE InitRenderer(
			/* [in] */ IVwGraphics *pvg,
			/* [in] */ BSTR bstrData) = 0;

		virtual HRESULT STDMETHODCALLTYPE FontIsValid( void) = 0;

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
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

	};

#else 	/* C style interface */

	typedef struct IRenderEngineVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IRenderEngine * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IRenderEngine * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IRenderEngine * This);

		HRESULT ( STDMETHODCALLTYPE *InitRenderer )(
			IRenderEngine * This,
			/* [in] */ IVwGraphics *pvg,
			/* [in] */ BSTR bstrData);

		HRESULT ( STDMETHODCALLTYPE *FontIsValid )(
			IRenderEngine * This);

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
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			IRenderEngine * This,
			/* [in] */ ILgWritingSystemFactory *pwsf);

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

#define IRenderEngine_FontIsValid(This)	\
	( (This)->lpVtbl -> FontIsValid(This) )

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

#define CLSID_FwGrEngine __uuidof(FwGrEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("F39F9433-F05A-4A19-8D1E-3C55DD607633")
FwGrEngine;
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
			__RPC__deref_out  void **ppvObject);

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


#define CLSID_FwGraphiteProcess __uuidof(FwGraphiteProcess)

#ifdef __cplusplus

class DECLSPEC_UUID("B56AEFB9-96B4-4415-8415-64CBF3826704")
FwGraphiteProcess;
#endif

#ifndef __ILgCharacterPropertyEngine_INTERFACE_DEFINED__
#define __ILgCharacterPropertyEngine_INTERFACE_DEFINED__

/* interface ILgCharacterPropertyEngine */
/* [unique][object][uuid] */


#define IID_ILgCharacterPropertyEngine __uuidof(ILgCharacterPropertyEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("890C5B18-6E95-438E-8ADE-A4FFADDF0684")
	ILgCharacterPropertyEngine : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_GeneralCategory(
			/* [in] */ int ch,
			/* [retval][out] */ LgGeneralCharCategory *pcc) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BidiCategory(
			/* [in] */ int ch,
			/* [retval][out] */ LgBidiCategory *pbic) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsLetter(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsWordForming(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPunctuation(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsNumber(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsSeparator(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsSymbol(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsMark(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsOther(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsUpper(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsLower(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsTitle(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsModifier(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsOtherLetter(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsOpen(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClose(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsWordMedial(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsControl(
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ToLowerCh(
			/* [in] */ int ch,
			/* [retval][out] */ int *pch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ToUpperCh(
			/* [in] */ int ch,
			/* [retval][out] */ int *pch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ToTitleCh(
			/* [in] */ int ch,
			/* [retval][out] */ int *pch) = 0;

		virtual HRESULT STDMETHODCALLTYPE ToLower(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE ToUpper(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE ToTitle(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE ToLowerRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE ToUpperRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE ToTitleRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsUserDefinedClass(
			/* [in] */ int ch,
			/* [in] */ int chClass,
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SoundAlikeKey(
			/* [in] */ BSTR bstrValue,
			/* [retval][out] */ BSTR *pbstrKey) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CharacterName(
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Decomposition(
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE DecompositionRgch(
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FullDecomp(
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrOut) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE FullDecompRgch(
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NumericValue(
			/* [in] */ int ch,
			/* [retval][out] */ int *pn) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CombiningClass(
			/* [in] */ int ch,
			/* [retval][out] */ int *pn) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Comment(
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetLineBreakProps(
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ byte *prglbOut) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetLineBreakStatus(
			/* [size_is][in] */ const byte *prglbpIn,
			/* [in] */ int cb,
			/* [size_is][out] */ byte *prglbsOut) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetLineBreakInfo(
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [size_is][out] */ byte *prglbsOut,
			/* [out] */ int *pichBreak) = 0;

		virtual HRESULT STDMETHODCALLTYPE StripDiacritics(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE StripDiacriticsRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE NormalizeKdRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut) = 0;

		virtual HRESULT STDMETHODCALLTYPE NormalizeD(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE NormalizeDRgch(
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Locale(
			/* [retval][out] */ int *pnLocale) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Locale(
			/* [in] */ int nLocale) = 0;

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

	typedef struct ILgCharacterPropertyEngineVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgCharacterPropertyEngine * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgCharacterPropertyEngine * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_GeneralCategory )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ LgGeneralCharCategory *pcc);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BidiCategory )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ LgBidiCategory *pbic);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsLetter )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWordForming )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPunctuation )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsNumber )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsSeparator )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsSymbol )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsMark )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOther )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsUpper )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsLower )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsTitle )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsModifier )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOtherLetter )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOpen )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClose )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWordMedial )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsControl )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToLowerCh )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToUpperCh )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToTitleCh )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		HRESULT ( STDMETHODCALLTYPE *ToLower )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *ToUpper )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *ToTitle )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToLowerRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToUpperRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToTitleRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsUserDefinedClass )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [in] */ int chClass,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SoundAlikeKey )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstrValue,
			/* [retval][out] */ BSTR *pbstrKey);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CharacterName )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Decomposition )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *DecompositionRgch )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FullDecomp )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *FullDecompRgch )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NumericValue )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CombiningClass )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Comment )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakProps )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ byte *prglbOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakStatus )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ const byte *prglbpIn,
			/* [in] */ int cb,
			/* [size_is][out] */ byte *prglbsOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakInfo )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [size_is][out] */ byte *prglbsOut,
			/* [out] */ int *pichBreak);

		HRESULT ( STDMETHODCALLTYPE *StripDiacritics )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *StripDiacriticsRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *NormalizeKdRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		HRESULT ( STDMETHODCALLTYPE *NormalizeD )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *NormalizeDRgch )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
			ILgCharacterPropertyEngine * This,
			/* [retval][out] */ int *pnLocale);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Locale )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int nLocale);

		HRESULT ( STDMETHODCALLTYPE *GetLineBreakText )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgchOut,
			/* [out] */ int *pcchOut);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LineBreakText )(
			ILgCharacterPropertyEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchMax);

		HRESULT ( STDMETHODCALLTYPE *LineBreakBefore )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ichIn,
			/* [out] */ int *pichOut,
			/* [out] */ LgLineBreak *plbWeight);

		HRESULT ( STDMETHODCALLTYPE *LineBreakAfter )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ int ichIn,
			/* [out] */ int *pichOut,
			/* [out] */ LgLineBreak *plbWeight);

		END_INTERFACE
	} ILgCharacterPropertyEngineVtbl;

	interface ILgCharacterPropertyEngine
	{
		CONST_VTBL struct ILgCharacterPropertyEngineVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgCharacterPropertyEngine_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgCharacterPropertyEngine_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgCharacterPropertyEngine_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgCharacterPropertyEngine_get_GeneralCategory(This,ch,pcc)	\
	( (This)->lpVtbl -> get_GeneralCategory(This,ch,pcc) )

#define ILgCharacterPropertyEngine_get_BidiCategory(This,ch,pbic)	\
	( (This)->lpVtbl -> get_BidiCategory(This,ch,pbic) )

#define ILgCharacterPropertyEngine_get_IsLetter(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsLetter(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsWordForming(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsWordForming(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsPunctuation(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsPunctuation(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsNumber(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsNumber(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsSeparator(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsSeparator(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsSymbol(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsSymbol(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsMark(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsMark(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsOther(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOther(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsUpper(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsUpper(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsLower(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsLower(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsTitle(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsTitle(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsModifier(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsModifier(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsOtherLetter(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOtherLetter(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsOpen(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOpen(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsClose(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsClose(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsWordMedial(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsWordMedial(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_IsControl(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsControl(This,ch,pfRet) )

#define ILgCharacterPropertyEngine_get_ToLowerCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToLowerCh(This,ch,pch) )

#define ILgCharacterPropertyEngine_get_ToUpperCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToUpperCh(This,ch,pch) )

#define ILgCharacterPropertyEngine_get_ToTitleCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToTitleCh(This,ch,pch) )

#define ILgCharacterPropertyEngine_ToLower(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToLower(This,bstr,pbstr) )

#define ILgCharacterPropertyEngine_ToUpper(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToUpper(This,bstr,pbstr) )

#define ILgCharacterPropertyEngine_ToTitle(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToTitle(This,bstr,pbstr) )

#define ILgCharacterPropertyEngine_ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgCharacterPropertyEngine_ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgCharacterPropertyEngine_ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgCharacterPropertyEngine_get_IsUserDefinedClass(This,ch,chClass,pfRet)	\
	( (This)->lpVtbl -> get_IsUserDefinedClass(This,ch,chClass,pfRet) )

#define ILgCharacterPropertyEngine_get_SoundAlikeKey(This,bstrValue,pbstrKey)	\
	( (This)->lpVtbl -> get_SoundAlikeKey(This,bstrValue,pbstrKey) )

#define ILgCharacterPropertyEngine_get_CharacterName(This,ch,pbstrName)	\
	( (This)->lpVtbl -> get_CharacterName(This,ch,pbstrName) )

#define ILgCharacterPropertyEngine_get_Decomposition(This,ch,pbstr)	\
	( (This)->lpVtbl -> get_Decomposition(This,ch,pbstr) )

#define ILgCharacterPropertyEngine_DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	( (This)->lpVtbl -> DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp) )

#define ILgCharacterPropertyEngine_get_FullDecomp(This,ch,pbstrOut)	\
	( (This)->lpVtbl -> get_FullDecomp(This,ch,pbstrOut) )

#define ILgCharacterPropertyEngine_FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	( (This)->lpVtbl -> FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp) )

#define ILgCharacterPropertyEngine_get_NumericValue(This,ch,pn)	\
	( (This)->lpVtbl -> get_NumericValue(This,ch,pn) )

#define ILgCharacterPropertyEngine_get_CombiningClass(This,ch,pn)	\
	( (This)->lpVtbl -> get_CombiningClass(This,ch,pn) )

#define ILgCharacterPropertyEngine_get_Comment(This,ch,pbstr)	\
	( (This)->lpVtbl -> get_Comment(This,ch,pbstr) )

#define ILgCharacterPropertyEngine_GetLineBreakProps(This,prgchIn,cchIn,prglbOut)	\
	( (This)->lpVtbl -> GetLineBreakProps(This,prgchIn,cchIn,prglbOut) )

#define ILgCharacterPropertyEngine_GetLineBreakStatus(This,prglbpIn,cb,prglbsOut)	\
	( (This)->lpVtbl -> GetLineBreakStatus(This,prglbpIn,cb,prglbsOut) )

#define ILgCharacterPropertyEngine_GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak)	\
	( (This)->lpVtbl -> GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak) )

#define ILgCharacterPropertyEngine_StripDiacritics(This,bstr,pbstr)	\
	( (This)->lpVtbl -> StripDiacritics(This,bstr,pbstr) )

#define ILgCharacterPropertyEngine_StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgCharacterPropertyEngine_NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgCharacterPropertyEngine_NormalizeD(This,bstr,pbstr)	\
	( (This)->lpVtbl -> NormalizeD(This,bstr,pbstr) )

#define ILgCharacterPropertyEngine_NormalizeDRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> NormalizeDRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgCharacterPropertyEngine_get_Locale(This,pnLocale)	\
	( (This)->lpVtbl -> get_Locale(This,pnLocale) )

#define ILgCharacterPropertyEngine_put_Locale(This,nLocale)	\
	( (This)->lpVtbl -> put_Locale(This,nLocale) )

#define ILgCharacterPropertyEngine_GetLineBreakText(This,cchMax,prgchOut,pcchOut)	\
	( (This)->lpVtbl -> GetLineBreakText(This,cchMax,prgchOut,pcchOut) )

#define ILgCharacterPropertyEngine_put_LineBreakText(This,prgchIn,cchMax)	\
	( (This)->lpVtbl -> put_LineBreakText(This,prgchIn,cchMax) )

#define ILgCharacterPropertyEngine_LineBreakBefore(This,ichIn,pichOut,plbWeight)	\
	( (This)->lpVtbl -> LineBreakBefore(This,ichIn,pichOut,plbWeight) )

#define ILgCharacterPropertyEngine_LineBreakAfter(This,ichIn,pichOut,plbWeight)	\
	( (This)->lpVtbl -> LineBreakAfter(This,ichIn,pichOut,plbWeight) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgCharacterPropertyEngine_INTERFACE_DEFINED__ */


#ifndef __ILgStringConverter_INTERFACE_DEFINED__
#define __ILgStringConverter_INTERFACE_DEFINED__

/* interface ILgStringConverter */
/* [unique][object][uuid] */


#define IID_ILgStringConverter __uuidof(ILgStringConverter)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8BE2C911-6A81-48B5-A27F-B8CE63983082")
	ILgStringConverter : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ConvertString(
			/* [in] */ BSTR bstrIn,
			/* [retval][out] */ BSTR *pbstrOut) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE ConvertStringRgch(
			/* [size_is][in] */ const OLECHAR *prgch,
			/* [in] */ int cch,
			/* [in] */ int cchMax,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [out] */ int *pcchOut) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgStringConverterVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgStringConverter * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgStringConverter * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgStringConverter * This);

		HRESULT ( STDMETHODCALLTYPE *ConvertString )(
			ILgStringConverter * This,
			/* [in] */ BSTR bstrIn,
			/* [retval][out] */ BSTR *pbstrOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ConvertStringRgch )(
			ILgStringConverter * This,
			/* [size_is][in] */ const OLECHAR *prgch,
			/* [in] */ int cch,
			/* [in] */ int cchMax,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [out] */ int *pcchOut);

		END_INTERFACE
	} ILgStringConverterVtbl;

	interface ILgStringConverter
	{
		CONST_VTBL struct ILgStringConverterVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgStringConverter_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgStringConverter_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgStringConverter_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgStringConverter_ConvertString(This,bstrIn,pbstrOut)	\
	( (This)->lpVtbl -> ConvertString(This,bstrIn,pbstrOut) )

#define ILgStringConverter_ConvertStringRgch(This,prgch,cch,cchMax,prgchOut,pcchOut)	\
	( (This)->lpVtbl -> ConvertStringRgch(This,prgch,cch,cchMax,prgchOut,pcchOut) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgStringConverter_INTERFACE_DEFINED__ */


#ifndef __ILgTokenizer_INTERFACE_DEFINED__
#define __ILgTokenizer_INTERFACE_DEFINED__

/* interface ILgTokenizer */
/* [unique][object][uuid] */


#define IID_ILgTokenizer __uuidof(ILgTokenizer)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("577C6DA1-CFC1-4AFB-82B2-AF818EC2FE9F")
	ILgTokenizer : public IUnknown
	{
	public:
		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetToken(
			/* [size_is][in] */ OLECHAR *prgchInput,
			/* [in] */ int cch,
			/* [out] */ int *pichMin,
			/* [out] */ int *pichLim) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TokenStart(
			/* [in] */ BSTR bstrInput,
			/* [in] */ int ichFirst,
			/* [retval][out] */ int *pichMin) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TokenEnd(
			/* [in] */ BSTR bstrInput,
			/* [in] */ int ichFirst,
			/* [retval][out] */ int *pichLim) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgTokenizerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgTokenizer * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgTokenizer * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgTokenizer * This);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetToken )(
			ILgTokenizer * This,
			/* [size_is][in] */ OLECHAR *prgchInput,
			/* [in] */ int cch,
			/* [out] */ int *pichMin,
			/* [out] */ int *pichLim);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TokenStart )(
			ILgTokenizer * This,
			/* [in] */ BSTR bstrInput,
			/* [in] */ int ichFirst,
			/* [retval][out] */ int *pichMin);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TokenEnd )(
			ILgTokenizer * This,
			/* [in] */ BSTR bstrInput,
			/* [in] */ int ichFirst,
			/* [retval][out] */ int *pichLim);

		END_INTERFACE
	} ILgTokenizerVtbl;

	interface ILgTokenizer
	{
		CONST_VTBL struct ILgTokenizerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgTokenizer_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgTokenizer_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgTokenizer_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgTokenizer_GetToken(This,prgchInput,cch,pichMin,pichLim)	\
	( (This)->lpVtbl -> GetToken(This,prgchInput,cch,pichMin,pichLim) )

#define ILgTokenizer_get_TokenStart(This,bstrInput,ichFirst,pichMin)	\
	( (This)->lpVtbl -> get_TokenStart(This,bstrInput,ichFirst,pichMin) )

#define ILgTokenizer_get_TokenEnd(This,bstrInput,ichFirst,pichLim)	\
	( (This)->lpVtbl -> get_TokenEnd(This,bstrInput,ichFirst,pichLim) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgTokenizer_INTERFACE_DEFINED__ */


#ifndef __ILgSpellCheckFactory_INTERFACE_DEFINED__
#define __ILgSpellCheckFactory_INTERFACE_DEFINED__

/* interface ILgSpellCheckFactory */
/* [unique][object][uuid] */


#define IID_ILgSpellCheckFactory __uuidof(ILgSpellCheckFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("9F9298F5-FD41-44B0-83BA-BED9F56CF974")
	ILgSpellCheckFactory : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Checker(
			/* [retval][out] */ ILgSpellChecker **ppspchk) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgSpellCheckFactoryVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgSpellCheckFactory * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgSpellCheckFactory * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgSpellCheckFactory * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Checker )(
			ILgSpellCheckFactory * This,
			/* [retval][out] */ ILgSpellChecker **ppspchk);

		END_INTERFACE
	} ILgSpellCheckFactoryVtbl;

	interface ILgSpellCheckFactory
	{
		CONST_VTBL struct ILgSpellCheckFactoryVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgSpellCheckFactory_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgSpellCheckFactory_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgSpellCheckFactory_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgSpellCheckFactory_get_Checker(This,ppspchk)	\
	( (This)->lpVtbl -> get_Checker(This,ppspchk) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgSpellCheckFactory_INTERFACE_DEFINED__ */


#ifndef __ILgSpellChecker_INTERFACE_DEFINED__
#define __ILgSpellChecker_INTERFACE_DEFINED__

/* interface ILgSpellChecker */
/* [unique][object][uuid] */


#define IID_ILgSpellChecker __uuidof(ILgSpellChecker)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("E3661AF5-26C6-4907-9243-610DAD84D9D4")
	ILgSpellChecker : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ LPCOLESTR pszwCustom) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetOptions(
			/* [in] */ int grfsplc) = 0;

		virtual HRESULT STDMETHODCALLTYPE Check(
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ int cchw,
			/* [out] */ int *pichMinBad,
			/* [out] */ int *pichLimBad,
			/* [out] */ BSTR *pbstrBad,
			/* [out] */ BSTR *pbstrSuggest,
			/* [out] */ int *pscrs) = 0;

		virtual HRESULT STDMETHODCALLTYPE Suggest(
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ int cchw,
			/* [in] */ ComBool fFirst,
			/* [out] */ BSTR *pbstrSuggest) = 0;

		virtual HRESULT STDMETHODCALLTYPE IgnoreAll(
			/* [in] */ LPCOLESTR pszw) = 0;

		virtual HRESULT STDMETHODCALLTYPE Change(
			/* [in] */ LPCOLESTR pszwSrc,
			/* [in] */ LPCOLESTR pszwDst,
			ComBool fAll) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddToUser(
			/* [in] */ LPCOLESTR pszw) = 0;

		virtual HRESULT STDMETHODCALLTYPE FlushIgnoreList( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE FlushChangeList(
			/* [in] */ ComBool fAll) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgSpellCheckerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgSpellChecker * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgSpellChecker * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgSpellChecker * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgSpellChecker * This,
			/* [in] */ LPCOLESTR pszwCustom);

		HRESULT ( STDMETHODCALLTYPE *SetOptions )(
			ILgSpellChecker * This,
			/* [in] */ int grfsplc);

		HRESULT ( STDMETHODCALLTYPE *Check )(
			ILgSpellChecker * This,
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ int cchw,
			/* [out] */ int *pichMinBad,
			/* [out] */ int *pichLimBad,
			/* [out] */ BSTR *pbstrBad,
			/* [out] */ BSTR *pbstrSuggest,
			/* [out] */ int *pscrs);

		HRESULT ( STDMETHODCALLTYPE *Suggest )(
			ILgSpellChecker * This,
			/* [size_is][in] */ const OLECHAR *prgchw,
			/* [in] */ int cchw,
			/* [in] */ ComBool fFirst,
			/* [out] */ BSTR *pbstrSuggest);

		HRESULT ( STDMETHODCALLTYPE *IgnoreAll )(
			ILgSpellChecker * This,
			/* [in] */ LPCOLESTR pszw);

		HRESULT ( STDMETHODCALLTYPE *Change )(
			ILgSpellChecker * This,
			/* [in] */ LPCOLESTR pszwSrc,
			/* [in] */ LPCOLESTR pszwDst,
			ComBool fAll);

		HRESULT ( STDMETHODCALLTYPE *AddToUser )(
			ILgSpellChecker * This,
			/* [in] */ LPCOLESTR pszw);

		HRESULT ( STDMETHODCALLTYPE *FlushIgnoreList )(
			ILgSpellChecker * This);

		HRESULT ( STDMETHODCALLTYPE *FlushChangeList )(
			ILgSpellChecker * This,
			/* [in] */ ComBool fAll);

		END_INTERFACE
	} ILgSpellCheckerVtbl;

	interface ILgSpellChecker
	{
		CONST_VTBL struct ILgSpellCheckerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgSpellChecker_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgSpellChecker_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgSpellChecker_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgSpellChecker_Init(This,pszwCustom)	\
	( (This)->lpVtbl -> Init(This,pszwCustom) )

#define ILgSpellChecker_SetOptions(This,grfsplc)	\
	( (This)->lpVtbl -> SetOptions(This,grfsplc) )

#define ILgSpellChecker_Check(This,prgchw,cchw,pichMinBad,pichLimBad,pbstrBad,pbstrSuggest,pscrs)	\
	( (This)->lpVtbl -> Check(This,prgchw,cchw,pichMinBad,pichLimBad,pbstrBad,pbstrSuggest,pscrs) )

#define ILgSpellChecker_Suggest(This,prgchw,cchw,fFirst,pbstrSuggest)	\
	( (This)->lpVtbl -> Suggest(This,prgchw,cchw,fFirst,pbstrSuggest) )

#define ILgSpellChecker_IgnoreAll(This,pszw)	\
	( (This)->lpVtbl -> IgnoreAll(This,pszw) )

#define ILgSpellChecker_Change(This,pszwSrc,pszwDst,fAll)	\
	( (This)->lpVtbl -> Change(This,pszwSrc,pszwDst,fAll) )

#define ILgSpellChecker_AddToUser(This,pszw)	\
	( (This)->lpVtbl -> AddToUser(This,pszw) )

#define ILgSpellChecker_FlushIgnoreList(This)	\
	( (This)->lpVtbl -> FlushIgnoreList(This) )

#define ILgSpellChecker_FlushChangeList(This,fAll)	\
	( (This)->lpVtbl -> FlushChangeList(This,fAll) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgSpellChecker_INTERFACE_DEFINED__ */


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
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

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
			__RPC__deref_out  void **ppvObject);

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
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			ILgCollatingEngine * This,
			/* [in] */ ILgWritingSystemFactory *pwsf);

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


#ifndef __ILgSearchEngine_INTERFACE_DEFINED__
#define __ILgSearchEngine_INTERFACE_DEFINED__

/* interface ILgSearchEngine */
/* [unique][object][uuid] */


#define IID_ILgSearchEngine __uuidof(ILgSearchEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("09FCA8D5-5BF6-4BFF-A317-E0126410D79A")
	ILgSearchEngine : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetPattern(
			/* [in] */ BSTR bstrPattern,
			/* [in] */ ComBool fIgnoreCase,
			/* [in] */ ComBool fIgnoreModifiers,
			/* [in] */ ComBool fUseSoundAlike,
			/* [in] */ ComBool fUseWildCards) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetReplacePattern(
			/* [in] */ BSTR bstrPattern) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShowPatternDialog(
			/* [in] */ BSTR bstrTitle,
			/* [in] */ ILgWritingSystem *pwse,
			/* [in] */ ComBool fForReplace,
			/* [retval][out] */ ComBool *pfGoAhead) = 0;

		virtual HRESULT STDMETHODCALLTYPE FindString(
			/* [in] */ BSTR bstrSource,
			/* [in] */ int ichFirst,
			/* [out] */ int *ichMinFound,
			/* [out] */ int *ichLimFound,
			/* [retval][out] */ ComBool *pfFound) = 0;

		virtual HRESULT STDMETHODCALLTYPE FindReplace(
			/* [in] */ BSTR bstrSource,
			/* [in] */ int ichFirst,
			/* [out] */ int *ichMinFound,
			/* [out] */ int *ichLimFound,
			/* [retval][out] */ BSTR *pbstrReplacement) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgSearchEngineVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgSearchEngine * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgSearchEngine * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgSearchEngine * This);

		HRESULT ( STDMETHODCALLTYPE *SetPattern )(
			ILgSearchEngine * This,
			/* [in] */ BSTR bstrPattern,
			/* [in] */ ComBool fIgnoreCase,
			/* [in] */ ComBool fIgnoreModifiers,
			/* [in] */ ComBool fUseSoundAlike,
			/* [in] */ ComBool fUseWildCards);

		HRESULT ( STDMETHODCALLTYPE *SetReplacePattern )(
			ILgSearchEngine * This,
			/* [in] */ BSTR bstrPattern);

		HRESULT ( STDMETHODCALLTYPE *ShowPatternDialog )(
			ILgSearchEngine * This,
			/* [in] */ BSTR bstrTitle,
			/* [in] */ ILgWritingSystem *pwse,
			/* [in] */ ComBool fForReplace,
			/* [retval][out] */ ComBool *pfGoAhead);

		HRESULT ( STDMETHODCALLTYPE *FindString )(
			ILgSearchEngine * This,
			/* [in] */ BSTR bstrSource,
			/* [in] */ int ichFirst,
			/* [out] */ int *ichMinFound,
			/* [out] */ int *ichLimFound,
			/* [retval][out] */ ComBool *pfFound);

		HRESULT ( STDMETHODCALLTYPE *FindReplace )(
			ILgSearchEngine * This,
			/* [in] */ BSTR bstrSource,
			/* [in] */ int ichFirst,
			/* [out] */ int *ichMinFound,
			/* [out] */ int *ichLimFound,
			/* [retval][out] */ BSTR *pbstrReplacement);

		END_INTERFACE
	} ILgSearchEngineVtbl;

	interface ILgSearchEngine
	{
		CONST_VTBL struct ILgSearchEngineVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgSearchEngine_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgSearchEngine_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgSearchEngine_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgSearchEngine_SetPattern(This,bstrPattern,fIgnoreCase,fIgnoreModifiers,fUseSoundAlike,fUseWildCards)	\
	( (This)->lpVtbl -> SetPattern(This,bstrPattern,fIgnoreCase,fIgnoreModifiers,fUseSoundAlike,fUseWildCards) )

#define ILgSearchEngine_SetReplacePattern(This,bstrPattern)	\
	( (This)->lpVtbl -> SetReplacePattern(This,bstrPattern) )

#define ILgSearchEngine_ShowPatternDialog(This,bstrTitle,pwse,fForReplace,pfGoAhead)	\
	( (This)->lpVtbl -> ShowPatternDialog(This,bstrTitle,pwse,fForReplace,pfGoAhead) )

#define ILgSearchEngine_FindString(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pfFound)	\
	( (This)->lpVtbl -> FindString(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pfFound) )

#define ILgSearchEngine_FindReplace(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pbstrReplacement)	\
	( (This)->lpVtbl -> FindReplace(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pbstrReplacement) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgSearchEngine_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystem_INTERFACE_DEFINED__
#define __ILgWritingSystem_INTERFACE_DEFINED__

/* interface ILgWritingSystem */
/* [unique][object][uuid] */


#define IID_ILgWritingSystem __uuidof(ILgWritingSystem)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("9F74A170-E8BB-466d-8848-5FDB28AC5AF8")
	ILgWritingSystem : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Id(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Handle(
			/* [retval][out] */ int *pws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LanguageName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ISO3(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LCID(
			/* [retval][out] */ int *pnLocale) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_LCID(
			/* [in] */ int nLocale) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SpellCheckingId(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SpellCheckingId(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RightToLeftScript(
			/* [retval][out] */ ComBool *pfRightToLeft) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_RightToLeftScript(
			/* [in] */ ComBool fRightToLeft) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Renderer(
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppreneng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultFontFeatures(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DefaultFontFeatures(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultFontName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DefaultFontName(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CharPropEngine(
			/* [retval][out] */ ILgCharacterPropertyEngine **pppropeng) = 0;

		virtual HRESULT STDMETHODCALLTYPE InterpretChrp(
			/* [out][in] */ LgCharRenderProps *pchrp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Keyboard(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Keyboard(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CurrentLCID(
			/* [retval][out] */ int *pnLangId) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_CurrentLCID(
			/* [in] */ int nLangId) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgWritingSystemVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgWritingSystem * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

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

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ISO3 )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LCID )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pnLocale);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LCID )(
			ILgWritingSystem * This,
			/* [in] */ int nLocale);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SpellCheckingId )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SpellCheckingId )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RightToLeftScript )(
			ILgWritingSystem * This,
			/* [retval][out] */ ComBool *pfRightToLeft);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_RightToLeftScript )(
			ILgWritingSystem * This,
			/* [in] */ ComBool fRightToLeft);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Renderer )(
			ILgWritingSystem * This,
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppreneng);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultFontFeatures )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DefaultFontFeatures )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultFontName )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DefaultFontName )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CharPropEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgCharacterPropertyEngine **pppropeng);

		HRESULT ( STDMETHODCALLTYPE *InterpretChrp )(
			ILgWritingSystem * This,
			/* [out][in] */ LgCharRenderProps *pchrp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Keyboard )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Keyboard )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CurrentLCID )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pnLangId);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_CurrentLCID )(
			ILgWritingSystem * This,
			/* [in] */ int nLangId);

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

#define ILgWritingSystem_get_ISO3(This,pbstr)	\
	( (This)->lpVtbl -> get_ISO3(This,pbstr) )

#define ILgWritingSystem_get_LCID(This,pnLocale)	\
	( (This)->lpVtbl -> get_LCID(This,pnLocale) )

#define ILgWritingSystem_put_LCID(This,nLocale)	\
	( (This)->lpVtbl -> put_LCID(This,nLocale) )

#define ILgWritingSystem_get_SpellCheckingId(This,pbstr)	\
	( (This)->lpVtbl -> get_SpellCheckingId(This,pbstr) )

#define ILgWritingSystem_put_SpellCheckingId(This,bstr)	\
	( (This)->lpVtbl -> put_SpellCheckingId(This,bstr) )

#define ILgWritingSystem_get_RightToLeftScript(This,pfRightToLeft)	\
	( (This)->lpVtbl -> get_RightToLeftScript(This,pfRightToLeft) )

#define ILgWritingSystem_put_RightToLeftScript(This,fRightToLeft)	\
	( (This)->lpVtbl -> put_RightToLeftScript(This,fRightToLeft) )

#define ILgWritingSystem_get_Renderer(This,pvg,ppreneng)	\
	( (This)->lpVtbl -> get_Renderer(This,pvg,ppreneng) )

#define ILgWritingSystem_get_DefaultFontFeatures(This,pbstr)	\
	( (This)->lpVtbl -> get_DefaultFontFeatures(This,pbstr) )

#define ILgWritingSystem_put_DefaultFontFeatures(This,bstr)	\
	( (This)->lpVtbl -> put_DefaultFontFeatures(This,bstr) )

#define ILgWritingSystem_get_DefaultFontName(This,pbstr)	\
	( (This)->lpVtbl -> get_DefaultFontName(This,pbstr) )

#define ILgWritingSystem_put_DefaultFontName(This,bstr)	\
	( (This)->lpVtbl -> put_DefaultFontName(This,bstr) )

#define ILgWritingSystem_get_CharPropEngine(This,pppropeng)	\
	( (This)->lpVtbl -> get_CharPropEngine(This,pppropeng) )

#define ILgWritingSystem_InterpretChrp(This,pchrp)	\
	( (This)->lpVtbl -> InterpretChrp(This,pchrp) )

#define ILgWritingSystem_get_Keyboard(This,pbstr)	\
	( (This)->lpVtbl -> get_Keyboard(This,pbstr) )

#define ILgWritingSystem_put_Keyboard(This,bstr)	\
	( (This)->lpVtbl -> put_Keyboard(This,bstr) )

#define ILgWritingSystem_get_CurrentLCID(This,pnLangId)	\
	( (This)->lpVtbl -> get_CurrentLCID(This,pnLangId) )

#define ILgWritingSystem_put_CurrentLCID(This,nLangId)	\
	( (This)->lpVtbl -> put_CurrentLCID(This,nLangId) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgWritingSystem_INTERFACE_DEFINED__ */


#ifndef __ILgTextServices_INTERFACE_DEFINED__
#define __ILgTextServices_INTERFACE_DEFINED__

/* interface ILgTextServices */
/* [unique][object][uuid] */


#define IID_ILgTextServices __uuidof(ILgTextServices)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("5B6303DE-E635-4DD7-A7FC-345BEEF352D8")
	ILgTextServices : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetKeyboard(
			/* [in] */ int nLcid,
			/* [in] */ BSTR bstrKeymanKbd,
			/* [out][in] */ int *pnActiveLangId,
			/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
			/* [out][in] */ ComBool *pfSelectLangPending) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgTextServicesVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgTextServices * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgTextServices * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgTextServices * This);

		HRESULT ( STDMETHODCALLTYPE *SetKeyboard )(
			ILgTextServices * This,
			/* [in] */ int nLcid,
			/* [in] */ BSTR bstrKeymanKbd,
			/* [out][in] */ int *pnActiveLangId,
			/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
			/* [out][in] */ ComBool *pfSelectLangPending);

		END_INTERFACE
	} ILgTextServicesVtbl;

	interface ILgTextServices
	{
		CONST_VTBL struct ILgTextServicesVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgTextServices_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgTextServices_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgTextServices_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgTextServices_SetKeyboard(This,nLcid,bstrKeymanKbd,pnActiveLangId,pbstrActiveKeymanKbd,pfSelectLangPending)	\
	( (This)->lpVtbl -> SetKeyboard(This,nLcid,bstrKeymanKbd,pnActiveLangId,pbstrActiveKeymanKbd,pfSelectLangPending) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgTextServices_INTERFACE_DEFINED__ */


#ifndef __ILgFontManager_INTERFACE_DEFINED__
#define __ILgFontManager_INTERFACE_DEFINED__

/* interface ILgFontManager */
/* [unique][object][uuid] */


#define IID_ILgFontManager __uuidof(ILgFontManager)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("73718689-B701-4241-A408-4C389ECD6664")
	ILgFontManager : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE IsFontAvailable(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfAvail) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsFontAvailableRgch(
			/* [in] */ int cch,
			/* [in] */ OLECHAR *prgchName,
			/* [retval][out] */ ComBool *pfAvail) = 0;

		virtual HRESULT STDMETHODCALLTYPE AvailableFonts(
			/* [out] */ BSTR *pbstrNames) = 0;

		virtual HRESULT STDMETHODCALLTYPE RefreshFontList( void) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgFontManagerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgFontManager * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgFontManager * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgFontManager * This);

		HRESULT ( STDMETHODCALLTYPE *IsFontAvailable )(
			ILgFontManager * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfAvail);

		HRESULT ( STDMETHODCALLTYPE *IsFontAvailableRgch )(
			ILgFontManager * This,
			/* [in] */ int cch,
			/* [in] */ OLECHAR *prgchName,
			/* [retval][out] */ ComBool *pfAvail);

		HRESULT ( STDMETHODCALLTYPE *AvailableFonts )(
			ILgFontManager * This,
			/* [out] */ BSTR *pbstrNames);

		HRESULT ( STDMETHODCALLTYPE *RefreshFontList )(
			ILgFontManager * This);

		END_INTERFACE
	} ILgFontManagerVtbl;

	interface ILgFontManager
	{
		CONST_VTBL struct ILgFontManagerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgFontManager_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgFontManager_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgFontManager_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgFontManager_IsFontAvailable(This,bstrName,pfAvail)	\
	( (This)->lpVtbl -> IsFontAvailable(This,bstrName,pfAvail) )

#define ILgFontManager_IsFontAvailableRgch(This,cch,prgchName,pfAvail)	\
	( (This)->lpVtbl -> IsFontAvailableRgch(This,cch,prgchName,pfAvail) )

#define ILgFontManager_AvailableFonts(This,pbstrNames)	\
	( (This)->lpVtbl -> AvailableFonts(This,pbstrNames) )

#define ILgFontManager_RefreshFontList(This)	\
	( (This)->lpVtbl -> RefreshFontList(This) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgFontManager_INTERFACE_DEFINED__ */


#define CLSID_LgInputMethodEditor __uuidof(LgInputMethodEditor)

#ifdef __cplusplus

class DECLSPEC_UUID("659C2C2F-7AF6-4F9E-AC6F-7A03C8418FC9")
LgInputMethodEditor;
#endif

#define CLSID_LgFontManager __uuidof(LgFontManager)

#ifdef __cplusplus

class DECLSPEC_UUID("02C3F580-796D-4B5F-BE43-166D97319DA5")
LgFontManager;
#endif

#ifndef __ILgIcuCharPropEngine_INTERFACE_DEFINED__
#define __ILgIcuCharPropEngine_INTERFACE_DEFINED__

/* interface ILgIcuCharPropEngine */
/* [unique][object][uuid] */


#define IID_ILgIcuCharPropEngine __uuidof(ILgIcuCharPropEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("E8689492-7622-427b-8518-6339294FD227")
	ILgIcuCharPropEngine : public ILgCharacterPropertyEngine
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ BSTR bstrLanguage,
			/* [in] */ BSTR bstrScript,
			/* [in] */ BSTR bstrCountry,
			/* [in] */ BSTR bstrVariant) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitCharOverrides(
			/* [in] */ BSTR bstrWsCharsList) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuCharPropEngineVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuCharPropEngine * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgIcuCharPropEngine * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgIcuCharPropEngine * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_GeneralCategory )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ LgGeneralCharCategory *pcc);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BidiCategory )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ LgBidiCategory *pbic);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsLetter )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWordForming )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPunctuation )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsNumber )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsSeparator )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsSymbol )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsMark )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOther )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsUpper )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsLower )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsTitle )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsModifier )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOtherLetter )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsOpen )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClose )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWordMedial )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsControl )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToLowerCh )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToUpperCh )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ToTitleCh )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pch);

		HRESULT ( STDMETHODCALLTYPE *ToLower )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *ToUpper )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *ToTitle )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToLowerRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToUpperRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *ToTitleRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchOut,
			/* [out] */ int *pcchRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsUserDefinedClass )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [in] */ int chClass,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SoundAlikeKey )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstrValue,
			/* [retval][out] */ BSTR *pbstrKey);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CharacterName )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Decomposition )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *DecompositionRgch )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FullDecomp )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstrOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *FullDecompRgch )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgch,
			/* [out] */ int *pcch,
			/* [out] */ ComBool *pfHasDecomp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NumericValue )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CombiningClass )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Comment )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ch,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakProps )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ byte *prglbOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakStatus )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ const byte *prglbpIn,
			/* [in] */ int cb,
			/* [size_is][out] */ byte *prglbsOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakInfo )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ const OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [size_is][out] */ byte *prglbsOut,
			/* [out] */ int *pichBreak);

		HRESULT ( STDMETHODCALLTYPE *StripDiacritics )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *StripDiacriticsRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *NormalizeKdRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		HRESULT ( STDMETHODCALLTYPE *NormalizeD )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *NormalizeDRgch )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchIn,
			/* [size_is][out] */ OLECHAR *prgchOut,
			/* [in] */ int cchMaxOut,
			/* [out] */ int *pcchOut);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
			ILgIcuCharPropEngine * This,
			/* [retval][out] */ int *pnLocale);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Locale )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int nLocale);

		HRESULT ( STDMETHODCALLTYPE *GetLineBreakText )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int cchMax,
			/* [out] */ OLECHAR *prgchOut,
			/* [out] */ int *pcchOut);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LineBreakText )(
			ILgIcuCharPropEngine * This,
			/* [size_is][in] */ OLECHAR *prgchIn,
			/* [in] */ int cchMax);

		HRESULT ( STDMETHODCALLTYPE *LineBreakBefore )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ichIn,
			/* [out] */ int *pichOut,
			/* [out] */ LgLineBreak *plbWeight);

		HRESULT ( STDMETHODCALLTYPE *LineBreakAfter )(
			ILgIcuCharPropEngine * This,
			/* [in] */ int ichIn,
			/* [out] */ int *pichOut,
			/* [out] */ LgLineBreak *plbWeight);

		HRESULT ( STDMETHODCALLTYPE *Initialize )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstrLanguage,
			/* [in] */ BSTR bstrScript,
			/* [in] */ BSTR bstrCountry,
			/* [in] */ BSTR bstrVariant);

		HRESULT ( STDMETHODCALLTYPE *InitCharOverrides )(
			ILgIcuCharPropEngine * This,
			/* [in] */ BSTR bstrWsCharsList);

		END_INTERFACE
	} ILgIcuCharPropEngineVtbl;

	interface ILgIcuCharPropEngine
	{
		CONST_VTBL struct ILgIcuCharPropEngineVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuCharPropEngine_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgIcuCharPropEngine_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgIcuCharPropEngine_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgIcuCharPropEngine_get_GeneralCategory(This,ch,pcc)	\
	( (This)->lpVtbl -> get_GeneralCategory(This,ch,pcc) )

#define ILgIcuCharPropEngine_get_BidiCategory(This,ch,pbic)	\
	( (This)->lpVtbl -> get_BidiCategory(This,ch,pbic) )

#define ILgIcuCharPropEngine_get_IsLetter(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsLetter(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsWordForming(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsWordForming(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsPunctuation(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsPunctuation(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsNumber(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsNumber(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsSeparator(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsSeparator(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsSymbol(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsSymbol(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsMark(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsMark(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsOther(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOther(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsUpper(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsUpper(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsLower(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsLower(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsTitle(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsTitle(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsModifier(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsModifier(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsOtherLetter(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOtherLetter(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsOpen(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsOpen(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsClose(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsClose(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsWordMedial(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsWordMedial(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_IsControl(This,ch,pfRet)	\
	( (This)->lpVtbl -> get_IsControl(This,ch,pfRet) )

#define ILgIcuCharPropEngine_get_ToLowerCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToLowerCh(This,ch,pch) )

#define ILgIcuCharPropEngine_get_ToUpperCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToUpperCh(This,ch,pch) )

#define ILgIcuCharPropEngine_get_ToTitleCh(This,ch,pch)	\
	( (This)->lpVtbl -> get_ToTitleCh(This,ch,pch) )

#define ILgIcuCharPropEngine_ToLower(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToLower(This,bstr,pbstr) )

#define ILgIcuCharPropEngine_ToUpper(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToUpper(This,bstr,pbstr) )

#define ILgIcuCharPropEngine_ToTitle(This,bstr,pbstr)	\
	( (This)->lpVtbl -> ToTitle(This,bstr,pbstr) )

#define ILgIcuCharPropEngine_ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgIcuCharPropEngine_ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgIcuCharPropEngine_ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	( (This)->lpVtbl -> ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet) )

#define ILgIcuCharPropEngine_get_IsUserDefinedClass(This,ch,chClass,pfRet)	\
	( (This)->lpVtbl -> get_IsUserDefinedClass(This,ch,chClass,pfRet) )

#define ILgIcuCharPropEngine_get_SoundAlikeKey(This,bstrValue,pbstrKey)	\
	( (This)->lpVtbl -> get_SoundAlikeKey(This,bstrValue,pbstrKey) )

#define ILgIcuCharPropEngine_get_CharacterName(This,ch,pbstrName)	\
	( (This)->lpVtbl -> get_CharacterName(This,ch,pbstrName) )

#define ILgIcuCharPropEngine_get_Decomposition(This,ch,pbstr)	\
	( (This)->lpVtbl -> get_Decomposition(This,ch,pbstr) )

#define ILgIcuCharPropEngine_DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	( (This)->lpVtbl -> DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp) )

#define ILgIcuCharPropEngine_get_FullDecomp(This,ch,pbstrOut)	\
	( (This)->lpVtbl -> get_FullDecomp(This,ch,pbstrOut) )

#define ILgIcuCharPropEngine_FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	( (This)->lpVtbl -> FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp) )

#define ILgIcuCharPropEngine_get_NumericValue(This,ch,pn)	\
	( (This)->lpVtbl -> get_NumericValue(This,ch,pn) )

#define ILgIcuCharPropEngine_get_CombiningClass(This,ch,pn)	\
	( (This)->lpVtbl -> get_CombiningClass(This,ch,pn) )

#define ILgIcuCharPropEngine_get_Comment(This,ch,pbstr)	\
	( (This)->lpVtbl -> get_Comment(This,ch,pbstr) )

#define ILgIcuCharPropEngine_GetLineBreakProps(This,prgchIn,cchIn,prglbOut)	\
	( (This)->lpVtbl -> GetLineBreakProps(This,prgchIn,cchIn,prglbOut) )

#define ILgIcuCharPropEngine_GetLineBreakStatus(This,prglbpIn,cb,prglbsOut)	\
	( (This)->lpVtbl -> GetLineBreakStatus(This,prglbpIn,cb,prglbsOut) )

#define ILgIcuCharPropEngine_GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak)	\
	( (This)->lpVtbl -> GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak) )

#define ILgIcuCharPropEngine_StripDiacritics(This,bstr,pbstr)	\
	( (This)->lpVtbl -> StripDiacritics(This,bstr,pbstr) )

#define ILgIcuCharPropEngine_StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgIcuCharPropEngine_NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgIcuCharPropEngine_NormalizeD(This,bstr,pbstr)	\
	( (This)->lpVtbl -> NormalizeD(This,bstr,pbstr) )

#define ILgIcuCharPropEngine_NormalizeDRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	( (This)->lpVtbl -> NormalizeDRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut) )

#define ILgIcuCharPropEngine_get_Locale(This,pnLocale)	\
	( (This)->lpVtbl -> get_Locale(This,pnLocale) )

#define ILgIcuCharPropEngine_put_Locale(This,nLocale)	\
	( (This)->lpVtbl -> put_Locale(This,nLocale) )

#define ILgIcuCharPropEngine_GetLineBreakText(This,cchMax,prgchOut,pcchOut)	\
	( (This)->lpVtbl -> GetLineBreakText(This,cchMax,prgchOut,pcchOut) )

#define ILgIcuCharPropEngine_put_LineBreakText(This,prgchIn,cchMax)	\
	( (This)->lpVtbl -> put_LineBreakText(This,prgchIn,cchMax) )

#define ILgIcuCharPropEngine_LineBreakBefore(This,ichIn,pichOut,plbWeight)	\
	( (This)->lpVtbl -> LineBreakBefore(This,ichIn,pichOut,plbWeight) )

#define ILgIcuCharPropEngine_LineBreakAfter(This,ichIn,pichOut,plbWeight)	\
	( (This)->lpVtbl -> LineBreakAfter(This,ichIn,pichOut,plbWeight) )


#define ILgIcuCharPropEngine_Initialize(This,bstrLanguage,bstrScript,bstrCountry,bstrVariant)	\
	( (This)->lpVtbl -> Initialize(This,bstrLanguage,bstrScript,bstrCountry,bstrVariant) )

#define ILgIcuCharPropEngine_InitCharOverrides(This,bstrWsCharsList)	\
	( (This)->lpVtbl -> InitCharOverrides(This,bstrWsCharsList) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgIcuCharPropEngine_INTERFACE_DEFINED__ */


#ifndef __ILgNumericEngine_INTERFACE_DEFINED__
#define __ILgNumericEngine_INTERFACE_DEFINED__

/* interface ILgNumericEngine */
/* [unique][object][uuid] */


#define IID_ILgNumericEngine __uuidof(ILgNumericEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("CBBF35E1-CE39-4EEC-AEBD-5B4AAAA52B6C")
	ILgNumericEngine : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntToString(
			/* [in] */ int n,
			/* [retval][out] */ BSTR *bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntToPrettyString(
			/* [in] */ int n,
			/* [retval][out] */ BSTR *bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringToInt(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ int *pn) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE StringToIntRgch(
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch,
			/* [out] */ int *pn,
			/* [out] */ int *pichUnused) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DblToString(
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DblToPrettyString(
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DblToExpString(
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringToDbl(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ double *pdbl) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE StringToDblRgch(
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch,
			/* [out] */ double *pdbl,
			/* [out] */ int *pichUnused) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgNumericEngineVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgNumericEngine * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgNumericEngine * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgNumericEngine * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntToString )(
			ILgNumericEngine * This,
			/* [in] */ int n,
			/* [retval][out] */ BSTR *bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntToPrettyString )(
			ILgNumericEngine * This,
			/* [in] */ int n,
			/* [retval][out] */ BSTR *bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringToInt )(
			ILgNumericEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ int *pn);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *StringToIntRgch )(
			ILgNumericEngine * This,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch,
			/* [out] */ int *pn,
			/* [out] */ int *pichUnused);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DblToString )(
			ILgNumericEngine * This,
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DblToPrettyString )(
			ILgNumericEngine * This,
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DblToExpString )(
			ILgNumericEngine * This,
			/* [in] */ double dbl,
			/* [in] */ int cchFracDigits,
			/* [retval][out] */ BSTR *bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringToDbl )(
			ILgNumericEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ double *pdbl);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *StringToDblRgch )(
			ILgNumericEngine * This,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch,
			/* [out] */ double *pdbl,
			/* [out] */ int *pichUnused);

		END_INTERFACE
	} ILgNumericEngineVtbl;

	interface ILgNumericEngine
	{
		CONST_VTBL struct ILgNumericEngineVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgNumericEngine_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgNumericEngine_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgNumericEngine_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgNumericEngine_get_IntToString(This,n,bstr)	\
	( (This)->lpVtbl -> get_IntToString(This,n,bstr) )

#define ILgNumericEngine_get_IntToPrettyString(This,n,bstr)	\
	( (This)->lpVtbl -> get_IntToPrettyString(This,n,bstr) )

#define ILgNumericEngine_get_StringToInt(This,bstr,pn)	\
	( (This)->lpVtbl -> get_StringToInt(This,bstr,pn) )

#define ILgNumericEngine_StringToIntRgch(This,prgch,cch,pn,pichUnused)	\
	( (This)->lpVtbl -> StringToIntRgch(This,prgch,cch,pn,pichUnused) )

#define ILgNumericEngine_get_DblToString(This,dbl,cchFracDigits,bstr)	\
	( (This)->lpVtbl -> get_DblToString(This,dbl,cchFracDigits,bstr) )

#define ILgNumericEngine_get_DblToPrettyString(This,dbl,cchFracDigits,bstr)	\
	( (This)->lpVtbl -> get_DblToPrettyString(This,dbl,cchFracDigits,bstr) )

#define ILgNumericEngine_get_DblToExpString(This,dbl,cchFracDigits,bstr)	\
	( (This)->lpVtbl -> get_DblToExpString(This,dbl,cchFracDigits,bstr) )

#define ILgNumericEngine_get_StringToDbl(This,bstr,pdbl)	\
	( (This)->lpVtbl -> get_StringToDbl(This,bstr,pdbl) )

#define ILgNumericEngine_StringToDblRgch(This,prgch,cch,pdbl,pichUnused)	\
	( (This)->lpVtbl -> StringToDblRgch(This,prgch,cch,pdbl,pichUnused) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgNumericEngine_INTERFACE_DEFINED__ */


#ifndef __ILgKeymanHandler_INTERFACE_DEFINED__
#define __ILgKeymanHandler_INTERFACE_DEFINED__

/* interface ILgKeymanHandler */
/* [unique][object][uuid] */


#define IID_ILgKeymanHandler __uuidof(ILgKeymanHandler)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("3F42144B-509F-4def-8DD3-6D8D26677001")
	ILgKeymanHandler : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ ComBool fForce) = 0;

		virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NLayout(
			/* [retval][out] */ int *pclayout) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [in] */ int ilayout,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ActiveKeyboardName(
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ActiveKeyboardName(
			/* [in] */ BSTR bstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_KeymanWindowsMessage(
			/* [retval][out] */ int *pwm) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgKeymanHandlerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgKeymanHandler * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgKeymanHandler * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgKeymanHandler * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgKeymanHandler * This,
			/* [in] */ ComBool fForce);

		HRESULT ( STDMETHODCALLTYPE *Close )(
			ILgKeymanHandler * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NLayout )(
			ILgKeymanHandler * This,
			/* [retval][out] */ int *pclayout);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			ILgKeymanHandler * This,
			/* [in] */ int ilayout,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ActiveKeyboardName )(
			ILgKeymanHandler * This,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ActiveKeyboardName )(
			ILgKeymanHandler * This,
			/* [in] */ BSTR bstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_KeymanWindowsMessage )(
			ILgKeymanHandler * This,
			/* [retval][out] */ int *pwm);

		END_INTERFACE
	} ILgKeymanHandlerVtbl;

	interface ILgKeymanHandler
	{
		CONST_VTBL struct ILgKeymanHandlerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgKeymanHandler_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgKeymanHandler_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgKeymanHandler_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgKeymanHandler_Init(This,fForce)	\
	( (This)->lpVtbl -> Init(This,fForce) )

#define ILgKeymanHandler_Close(This)	\
	( (This)->lpVtbl -> Close(This) )

#define ILgKeymanHandler_get_NLayout(This,pclayout)	\
	( (This)->lpVtbl -> get_NLayout(This,pclayout) )

#define ILgKeymanHandler_get_Name(This,ilayout,pbstrName)	\
	( (This)->lpVtbl -> get_Name(This,ilayout,pbstrName) )

#define ILgKeymanHandler_get_ActiveKeyboardName(This,pbstrName)	\
	( (This)->lpVtbl -> get_ActiveKeyboardName(This,pbstrName) )

#define ILgKeymanHandler_put_ActiveKeyboardName(This,bstrName)	\
	( (This)->lpVtbl -> put_ActiveKeyboardName(This,bstrName) )

#define ILgKeymanHandler_get_KeymanWindowsMessage(This,pwm)	\
	( (This)->lpVtbl -> get_KeymanWindowsMessage(This,pwm) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgKeymanHandler_INTERFACE_DEFINED__ */


#ifndef __ILgCodePageEnumerator_INTERFACE_DEFINED__
#define __ILgCodePageEnumerator_INTERFACE_DEFINED__

/* interface ILgCodePageEnumerator */
/* [unique][object][uuid] */


#define IID_ILgCodePageEnumerator __uuidof(ILgCodePageEnumerator)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2CFCF4B7-2FFE-4CF8-91BE-FBB57CC7782A")
	ILgCodePageEnumerator : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Next(
			/* [out] */ int *pnId,
			/* [out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgCodePageEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgCodePageEnumerator * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgCodePageEnumerator * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgCodePageEnumerator * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgCodePageEnumerator * This);

		HRESULT ( STDMETHODCALLTYPE *Next )(
			ILgCodePageEnumerator * This,
			/* [out] */ int *pnId,
			/* [out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgCodePageEnumeratorVtbl;

	interface ILgCodePageEnumerator
	{
		CONST_VTBL struct ILgCodePageEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgCodePageEnumerator_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgCodePageEnumerator_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgCodePageEnumerator_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgCodePageEnumerator_Init(This)	\
	( (This)->lpVtbl -> Init(This) )

#define ILgCodePageEnumerator_Next(This,pnId,pbstrName)	\
	( (This)->lpVtbl -> Next(This,pnId,pbstrName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgCodePageEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgLanguageEnumerator_INTERFACE_DEFINED__
#define __ILgLanguageEnumerator_INTERFACE_DEFINED__

/* interface ILgLanguageEnumerator */
/* [unique][object][uuid] */


#define IID_ILgLanguageEnumerator __uuidof(ILgLanguageEnumerator)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("746A16E1-0C36-4268-A261-E8012B0D67C5")
	ILgLanguageEnumerator : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Next(
			/* [out] */ int *pnId,
			/* [out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgLanguageEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgLanguageEnumerator * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgLanguageEnumerator * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgLanguageEnumerator * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgLanguageEnumerator * This);

		HRESULT ( STDMETHODCALLTYPE *Next )(
			ILgLanguageEnumerator * This,
			/* [out] */ int *pnId,
			/* [out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgLanguageEnumeratorVtbl;

	interface ILgLanguageEnumerator
	{
		CONST_VTBL struct ILgLanguageEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgLanguageEnumerator_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgLanguageEnumerator_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgLanguageEnumerator_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgLanguageEnumerator_Init(This)	\
	( (This)->lpVtbl -> Init(This) )

#define ILgLanguageEnumerator_Next(This,pnId,pbstrName)	\
	( (This)->lpVtbl -> Next(This,pnId,pbstrName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgLanguageEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuConverterEnumerator_INTERFACE_DEFINED__
#define __ILgIcuConverterEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuConverterEnumerator */
/* [unique][object][uuid] */


#define IID_ILgIcuConverterEnumerator __uuidof(ILgIcuConverterEnumerator)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8E6D558E-8755-4EA1-9FF6-039D375312E9")
	ILgIcuConverterEnumerator : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count(
			/* [retval][out] */ int *pcconv) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConverterName(
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConverterId(
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuConverterEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuConverterEnumerator * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgIcuConverterEnumerator * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgIcuConverterEnumerator * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
			ILgIcuConverterEnumerator * This,
			/* [retval][out] */ int *pcconv);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ConverterName )(
			ILgIcuConverterEnumerator * This,
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ConverterId )(
			ILgIcuConverterEnumerator * This,
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgIcuConverterEnumeratorVtbl;

	interface ILgIcuConverterEnumerator
	{
		CONST_VTBL struct ILgIcuConverterEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuConverterEnumerator_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgIcuConverterEnumerator_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgIcuConverterEnumerator_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgIcuConverterEnumerator_get_Count(This,pcconv)	\
	( (This)->lpVtbl -> get_Count(This,pcconv) )

#define ILgIcuConverterEnumerator_get_ConverterName(This,iconv,pbstrName)	\
	( (This)->lpVtbl -> get_ConverterName(This,iconv,pbstrName) )

#define ILgIcuConverterEnumerator_get_ConverterId(This,iconv,pbstrName)	\
	( (This)->lpVtbl -> get_ConverterId(This,iconv,pbstrName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgIcuConverterEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__
#define __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuTransliteratorEnumerator */
/* [unique][object][uuid] */


#define IID_ILgIcuTransliteratorEnumerator __uuidof(ILgIcuTransliteratorEnumerator)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("50F2492C-6C46-48BA-8B7F-5F04153AB2CC")
	ILgIcuTransliteratorEnumerator : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count(
			/* [retval][out] */ int *pctrans) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TransliteratorName(
			/* [in] */ int itrans,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TransliteratorId(
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuTransliteratorEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuTransliteratorEnumerator * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgIcuTransliteratorEnumerator * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgIcuTransliteratorEnumerator * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
			ILgIcuTransliteratorEnumerator * This,
			/* [retval][out] */ int *pctrans);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TransliteratorName )(
			ILgIcuTransliteratorEnumerator * This,
			/* [in] */ int itrans,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TransliteratorId )(
			ILgIcuTransliteratorEnumerator * This,
			/* [in] */ int iconv,
			/* [retval][out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgIcuTransliteratorEnumeratorVtbl;

	interface ILgIcuTransliteratorEnumerator
	{
		CONST_VTBL struct ILgIcuTransliteratorEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuTransliteratorEnumerator_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgIcuTransliteratorEnumerator_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgIcuTransliteratorEnumerator_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgIcuTransliteratorEnumerator_get_Count(This,pctrans)	\
	( (This)->lpVtbl -> get_Count(This,pctrans) )

#define ILgIcuTransliteratorEnumerator_get_TransliteratorName(This,itrans,pbstrName)	\
	( (This)->lpVtbl -> get_TransliteratorName(This,itrans,pbstrName) )

#define ILgIcuTransliteratorEnumerator_get_TransliteratorId(This,iconv,pbstrName)	\
	( (This)->lpVtbl -> get_TransliteratorId(This,iconv,pbstrName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__
#define __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuLocaleEnumerator */
/* [unique][object][uuid] */


#define IID_ILgIcuLocaleEnumerator __uuidof(ILgIcuLocaleEnumerator)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("08F649D0-D8AB-447B-AAB6-21F85CFA743C")
	ILgIcuLocaleEnumerator : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count(
			/* [retval][out] */ int *pclocale) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Language(
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Country(
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Variant(
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DisplayName(
			/* [in] */ int iloc,
			/* [in] */ BSTR bstrLocaleName,
			/* [retval][out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuLocaleEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgIcuLocaleEnumerator * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgIcuLocaleEnumerator * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
			ILgIcuLocaleEnumerator * This,
			/* [retval][out] */ int *pclocale);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Language )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Country )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Variant )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ int iloc,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DisplayName )(
			ILgIcuLocaleEnumerator * This,
			/* [in] */ int iloc,
			/* [in] */ BSTR bstrLocaleName,
			/* [retval][out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgIcuLocaleEnumeratorVtbl;

	interface ILgIcuLocaleEnumerator
	{
		CONST_VTBL struct ILgIcuLocaleEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuLocaleEnumerator_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgIcuLocaleEnumerator_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgIcuLocaleEnumerator_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgIcuLocaleEnumerator_get_Count(This,pclocale)	\
	( (This)->lpVtbl -> get_Count(This,pclocale) )

#define ILgIcuLocaleEnumerator_get_Name(This,iloc,pbstrName)	\
	( (This)->lpVtbl -> get_Name(This,iloc,pbstrName) )

#define ILgIcuLocaleEnumerator_get_Language(This,iloc,pbstrName)	\
	( (This)->lpVtbl -> get_Language(This,iloc,pbstrName) )

#define ILgIcuLocaleEnumerator_get_Country(This,iloc,pbstrName)	\
	( (This)->lpVtbl -> get_Country(This,iloc,pbstrName) )

#define ILgIcuLocaleEnumerator_get_Variant(This,iloc,pbstrName)	\
	( (This)->lpVtbl -> get_Variant(This,iloc,pbstrName) )

#define ILgIcuLocaleEnumerator_get_DisplayName(This,iloc,bstrLocaleName,pbstrName)	\
	( (This)->lpVtbl -> get_DisplayName(This,iloc,bstrLocaleName,pbstrName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuResourceBundle_INTERFACE_DEFINED__
#define __ILgIcuResourceBundle_INTERFACE_DEFINED__

/* interface ILgIcuResourceBundle */
/* [unique][object][uuid] */


#define IID_ILgIcuResourceBundle __uuidof(ILgIcuResourceBundle)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("C243C72A-0D15-44D9-ABCB-A6E875A7659A")
	ILgIcuResourceBundle : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ BSTR bstrPath,
			/* [in] */ BSTR locale) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Key(
			/* [retval][out] */ BSTR *pbstrKey) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_String(
			/* [retval][out] */ BSTR *pbstrString) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_GetSubsection(
			/* [in] */ BSTR bstrSectionName,
			/* [retval][out] */ ILgIcuResourceBundle **pprb) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HasNext(
			/* [retval][out] */ ComBool *pfHasNext) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Next(
			/* [retval][out] */ ILgIcuResourceBundle **pprb) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Size(
			/* [retval][out] */ int *pcrb) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringEx(
			/* [in] */ int irb,
			/* [retval][out] */ BSTR *pbstr) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuResourceBundleVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuResourceBundle * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgIcuResourceBundle * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgIcuResourceBundle * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgIcuResourceBundle * This,
			/* [in] */ BSTR bstrPath,
			/* [in] */ BSTR locale);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Key )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ BSTR *pbstrKey);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_String )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ BSTR *pbstrString);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_GetSubsection )(
			ILgIcuResourceBundle * This,
			/* [in] */ BSTR bstrSectionName,
			/* [retval][out] */ ILgIcuResourceBundle **pprb);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HasNext )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ ComBool *pfHasNext);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Next )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ ILgIcuResourceBundle **pprb);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Size )(
			ILgIcuResourceBundle * This,
			/* [retval][out] */ int *pcrb);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringEx )(
			ILgIcuResourceBundle * This,
			/* [in] */ int irb,
			/* [retval][out] */ BSTR *pbstr);

		END_INTERFACE
	} ILgIcuResourceBundleVtbl;

	interface ILgIcuResourceBundle
	{
		CONST_VTBL struct ILgIcuResourceBundleVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuResourceBundle_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILgIcuResourceBundle_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILgIcuResourceBundle_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILgIcuResourceBundle_Init(This,bstrPath,locale)	\
	( (This)->lpVtbl -> Init(This,bstrPath,locale) )

#define ILgIcuResourceBundle_get_Key(This,pbstrKey)	\
	( (This)->lpVtbl -> get_Key(This,pbstrKey) )

#define ILgIcuResourceBundle_get_String(This,pbstrString)	\
	( (This)->lpVtbl -> get_String(This,pbstrString) )

#define ILgIcuResourceBundle_get_Name(This,pbstrName)	\
	( (This)->lpVtbl -> get_Name(This,pbstrName) )

#define ILgIcuResourceBundle_get_GetSubsection(This,bstrSectionName,pprb)	\
	( (This)->lpVtbl -> get_GetSubsection(This,bstrSectionName,pprb) )

#define ILgIcuResourceBundle_get_HasNext(This,pfHasNext)	\
	( (This)->lpVtbl -> get_HasNext(This,pfHasNext) )

#define ILgIcuResourceBundle_get_Next(This,pprb)	\
	( (This)->lpVtbl -> get_Next(This,pprb) )

#define ILgIcuResourceBundle_get_Size(This,pcrb)	\
	( (This)->lpVtbl -> get_Size(This,pcrb) )

#define ILgIcuResourceBundle_get_StringEx(This,irb,pbstr)	\
	( (This)->lpVtbl -> get_StringEx(This,irb,pbstr) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgIcuResourceBundle_INTERFACE_DEFINED__ */


#ifndef __IRegexMatcher_INTERFACE_DEFINED__
#define __IRegexMatcher_INTERFACE_DEFINED__

/* interface IRegexMatcher */
/* [unique][object][uuid] */


#define IID_IRegexMatcher __uuidof(IRegexMatcher)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("6C62CCF0-4EE1-493C-8092-319B6CFBEEBC")
	IRegexMatcher : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ BSTR bstrPattern,
			/* [in] */ ComBool fMatchCase) = 0;

		virtual HRESULT STDMETHODCALLTYPE Reset(
			/* [in] */ BSTR bstrInput) = 0;

		virtual HRESULT STDMETHODCALLTYPE Find(
			/* [in] */ int ich,
			/* [retval][out] */ ComBool *pfFound) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Start(
			/* [in] */ int igroup,
			/* [retval][out] */ int *pich) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_End(
			/* [in] */ int igroup,
			/* [retval][out] */ int *pich) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ErrorMessage(
			/* [retval][out] */ BSTR *pbstrMsg) = 0;

	};

#else 	/* C style interface */

	typedef struct IRegexMatcherVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IRegexMatcher * This,
			/* [in] */ REFIID riid,
			/* [annotation][iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IRegexMatcher * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IRegexMatcher * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IRegexMatcher * This,
			/* [in] */ BSTR bstrPattern,
			/* [in] */ ComBool fMatchCase);

		HRESULT ( STDMETHODCALLTYPE *Reset )(
			IRegexMatcher * This,
			/* [in] */ BSTR bstrInput);

		HRESULT ( STDMETHODCALLTYPE *Find )(
			IRegexMatcher * This,
			/* [in] */ int ich,
			/* [retval][out] */ ComBool *pfFound);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Start )(
			IRegexMatcher * This,
			/* [in] */ int igroup,
			/* [retval][out] */ int *pich);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_End )(
			IRegexMatcher * This,
			/* [in] */ int igroup,
			/* [retval][out] */ int *pich);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ErrorMessage )(
			IRegexMatcher * This,
			/* [retval][out] */ BSTR *pbstrMsg);

		END_INTERFACE
	} IRegexMatcherVtbl;

	interface IRegexMatcher
	{
		CONST_VTBL struct IRegexMatcherVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IRegexMatcher_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define IRegexMatcher_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define IRegexMatcher_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define IRegexMatcher_Init(This,bstrPattern,fMatchCase)	\
	( (This)->lpVtbl -> Init(This,bstrPattern,fMatchCase) )

#define IRegexMatcher_Reset(This,bstrInput)	\
	( (This)->lpVtbl -> Reset(This,bstrInput) )

#define IRegexMatcher_Find(This,ich,pfFound)	\
	( (This)->lpVtbl -> Find(This,ich,pfFound) )

#define IRegexMatcher_get_Start(This,igroup,pich)	\
	( (This)->lpVtbl -> get_Start(This,igroup,pich) )

#define IRegexMatcher_get_End(This,igroup,pich)	\
	( (This)->lpVtbl -> get_End(This,igroup,pich) )

#define IRegexMatcher_get_ErrorMessage(This,pbstrMsg)	\
	( (This)->lpVtbl -> get_ErrorMessage(This,pbstrMsg) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IRegexMatcher_INTERFACE_DEFINED__ */


#define CLSID_RegexMatcherWrapper __uuidof(RegexMatcherWrapper)

#ifdef __cplusplus

class DECLSPEC_UUID("13D5C6D3-39D9-4BDA-A3F8-A5CAF6A6940A")
RegexMatcherWrapper;
#endif

#define CLSID_LgSystemCollater __uuidof(LgSystemCollater)

#ifdef __cplusplus

class DECLSPEC_UUID("E361F805-C902-4306-A5D8-F7802B0E7365")
LgSystemCollater;
#endif

#define CLSID_LgUnicodeCollater __uuidof(LgUnicodeCollater)

#ifdef __cplusplus

class DECLSPEC_UUID("0D9900D2-1693-481F-AA70-7EA64F264EC4")
LgUnicodeCollater;
#endif

#define CLSID_LgIcuCollator __uuidof(LgIcuCollator)

#ifdef __cplusplus

class DECLSPEC_UUID("E771361C-FF54-4120-9525-98A0B7A9ACCF")
LgIcuCollator;
#endif

#define CLSID_LgIcuCharPropEngine __uuidof(LgIcuCharPropEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("30D75676-A10F-48FE-9627-EBF4061EA49D")
LgIcuCharPropEngine;
#endif

#define CLSID_LgCPWordTokenizer __uuidof(LgCPWordTokenizer)

#ifdef __cplusplus

class DECLSPEC_UUID("7CE7CE94-AC47-42A5-823F-2F8EF51A9007")
LgCPWordTokenizer;
#endif

#define CLSID_LgWfiSpellChecker __uuidof(LgWfiSpellChecker)

#ifdef __cplusplus

class DECLSPEC_UUID("818445E2-0282-4688-8BB7-147FAACFF73A")
LgWfiSpellChecker;
#endif

#define CLSID_LgMSWordSpellChecker __uuidof(LgMSWordSpellChecker)

#ifdef __cplusplus

class DECLSPEC_UUID("5CF96DA5-299E-4FC5-A990-2D2FCEE7834D")
LgMSWordSpellChecker;
#endif

#define CLSID_LgNumericEngine __uuidof(LgNumericEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("FF22A7AB-223E-4D04-B648-0AE40588261D")
LgNumericEngine;
#endif

#define CLSID_LgKeymanHandler __uuidof(LgKeymanHandler)

#ifdef __cplusplus

class DECLSPEC_UUID("69ACA99C-F852-4C2C-9B5F-FF83238A17A5")
LgKeymanHandler;
#endif

#define CLSID_LgTextServices __uuidof(LgTextServices)

#ifdef __cplusplus

class DECLSPEC_UUID("720485C5-E8D5-4761-92F0-F70D2B3CF980")
LgTextServices;
#endif

#define CLSID_LgCodePageEnumerator __uuidof(LgCodePageEnumerator)

#ifdef __cplusplus

class DECLSPEC_UUID("9045F113-8626-41C0-A61E-A73FBE5920D1")
LgCodePageEnumerator;
#endif

#define CLSID_LgLanguageEnumerator __uuidof(LgLanguageEnumerator)

#ifdef __cplusplus

class DECLSPEC_UUID("B887505B-74DE-4ADC-A1D9-5553428C8D02")
LgLanguageEnumerator;
#endif

#define CLSID_LgIcuConverterEnumerator __uuidof(LgIcuConverterEnumerator)

#ifdef __cplusplus

class DECLSPEC_UUID("9E729461-F80D-4796-BA17-086BC61907F1")
LgIcuConverterEnumerator;
#endif

#define CLSID_LgIcuTransliteratorEnumerator __uuidof(LgIcuTransliteratorEnumerator)

#ifdef __cplusplus

class DECLSPEC_UUID("3F1FD0A4-B2B1-4589-BC82-9CEF5BA84F4E")
LgIcuTransliteratorEnumerator;
#endif

#define CLSID_LgIcuResourceBundle __uuidof(LgIcuResourceBundle)

#ifdef __cplusplus

class DECLSPEC_UUID("0DD7FC1A-AB97-4A39-882C-269760D86619")
LgIcuResourceBundle;
#endif

#define CLSID_LgIcuLocaleEnumerator __uuidof(LgIcuLocaleEnumerator)

#ifdef __cplusplus

class DECLSPEC_UUID("E426656C-64F7-480E-92F4-D41A7BFFD066")
LgIcuLocaleEnumerator;
#endif
#endif /* __FwKernelLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
