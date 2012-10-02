

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:02:00 2006
 */
/* Compiler settings for C:\fw\Output\Common\LanguageTlb.idl:
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


#ifndef __LanguageTlb_h__
#define __LanguageTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IOleDbEncap_FWD_DEFINED__
#define __IOleDbEncap_FWD_DEFINED__
typedef interface IOleDbEncap IOleDbEncap;
#endif 	/* __IOleDbEncap_FWD_DEFINED__ */


#ifndef __ITsString_FWD_DEFINED__
#define __ITsString_FWD_DEFINED__
typedef interface ITsString ITsString;
#endif 	/* __ITsString_FWD_DEFINED__ */


#ifndef __IFwFldSpec_FWD_DEFINED__
#define __IFwFldSpec_FWD_DEFINED__
typedef interface IFwFldSpec IFwFldSpec;
#endif 	/* __IFwFldSpec_FWD_DEFINED__ */


#ifndef __IUndoGrouper_FWD_DEFINED__
#define __IUndoGrouper_FWD_DEFINED__
typedef interface IUndoGrouper IUndoGrouper;
#endif 	/* __IUndoGrouper_FWD_DEFINED__ */


#ifndef __IFwCustomExport_FWD_DEFINED__
#define __IFwCustomExport_FWD_DEFINED__
typedef interface IFwCustomExport IFwCustomExport;
#endif 	/* __IFwCustomExport_FWD_DEFINED__ */


#ifndef __IFwTool_FWD_DEFINED__
#define __IFwTool_FWD_DEFINED__
typedef interface IFwTool IFwTool;
#endif 	/* __IFwTool_FWD_DEFINED__ */


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


#ifndef __IAdvInd_FWD_DEFINED__
#define __IAdvInd_FWD_DEFINED__
typedef interface IAdvInd IAdvInd;
#endif 	/* __IAdvInd_FWD_DEFINED__ */


#ifndef __IAdvInd2_FWD_DEFINED__
#define __IAdvInd2_FWD_DEFINED__
typedef interface IAdvInd2 IAdvInd2;
#endif 	/* __IAdvInd2_FWD_DEFINED__ */


#ifndef __IAdvInd3_FWD_DEFINED__
#define __IAdvInd3_FWD_DEFINED__
typedef interface IAdvInd3 IAdvInd3;
#endif 	/* __IAdvInd3_FWD_DEFINED__ */


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


#ifndef __IHelpTopicProvider_FWD_DEFINED__
#define __IHelpTopicProvider_FWD_DEFINED__
typedef interface IHelpTopicProvider IHelpTopicProvider;
#endif 	/* __IHelpTopicProvider_FWD_DEFINED__ */


#ifndef __FwFldSpec_FWD_DEFINED__
#define __FwFldSpec_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwFldSpec FwFldSpec;
#else
typedef struct FwFldSpec FwFldSpec;
#endif /* __cplusplus */

#endif 	/* __FwFldSpec_FWD_DEFINED__ */


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


#ifndef __ITsStreamWrapper_FWD_DEFINED__
#define __ITsStreamWrapper_FWD_DEFINED__
typedef interface ITsStreamWrapper ITsStreamWrapper;
#endif 	/* __ITsStreamWrapper_FWD_DEFINED__ */


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


#ifndef __TsStreamWrapper_FWD_DEFINED__
#define __TsStreamWrapper_FWD_DEFINED__

#ifdef __cplusplus
typedef class TsStreamWrapper TsStreamWrapper;
#else
typedef struct TsStreamWrapper TsStreamWrapper;
#endif /* __cplusplus */

#endif 	/* __TsStreamWrapper_FWD_DEFINED__ */


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


#ifndef __ILgCollation_FWD_DEFINED__
#define __ILgCollation_FWD_DEFINED__
typedef interface ILgCollation ILgCollation;
#endif 	/* __ILgCollation_FWD_DEFINED__ */


#ifndef __ILgWritingSystem_FWD_DEFINED__
#define __ILgWritingSystem_FWD_DEFINED__
typedef interface ILgWritingSystem ILgWritingSystem;
#endif 	/* __ILgWritingSystem_FWD_DEFINED__ */


#ifndef __ILgTsStringPlusWss_FWD_DEFINED__
#define __ILgTsStringPlusWss_FWD_DEFINED__
typedef interface ILgTsStringPlusWss ILgTsStringPlusWss;
#endif 	/* __ILgTsStringPlusWss_FWD_DEFINED__ */


#ifndef __ILgTsDataObject_FWD_DEFINED__
#define __ILgTsDataObject_FWD_DEFINED__
typedef interface ILgTsDataObject ILgTsDataObject;
#endif 	/* __ILgTsDataObject_FWD_DEFINED__ */


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


#ifndef __ILgNumericEngine_FWD_DEFINED__
#define __ILgNumericEngine_FWD_DEFINED__
typedef interface ILgNumericEngine ILgNumericEngine;
#endif 	/* __ILgNumericEngine_FWD_DEFINED__ */


#ifndef __ILgWritingSystemFactoryBuilder_FWD_DEFINED__
#define __ILgWritingSystemFactoryBuilder_FWD_DEFINED__
typedef interface ILgWritingSystemFactoryBuilder ILgWritingSystemFactoryBuilder;
#endif 	/* __ILgWritingSystemFactoryBuilder_FWD_DEFINED__ */


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


#ifndef __LgCharacterPropertyEngine_FWD_DEFINED__
#define __LgCharacterPropertyEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgCharacterPropertyEngine LgCharacterPropertyEngine;
#else
typedef struct LgCharacterPropertyEngine LgCharacterPropertyEngine;
#endif /* __cplusplus */

#endif 	/* __LgCharacterPropertyEngine_FWD_DEFINED__ */


#ifndef __LgIcuCharPropEngine_FWD_DEFINED__
#define __LgIcuCharPropEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgIcuCharPropEngine LgIcuCharPropEngine;
#else
typedef struct LgIcuCharPropEngine LgIcuCharPropEngine;
#endif /* __cplusplus */

#endif 	/* __LgIcuCharPropEngine_FWD_DEFINED__ */


#ifndef __LgCharPropOverrideEngine_FWD_DEFINED__
#define __LgCharPropOverrideEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgCharPropOverrideEngine LgCharPropOverrideEngine;
#else
typedef struct LgCharPropOverrideEngine LgCharPropOverrideEngine;
#endif /* __cplusplus */

#endif 	/* __LgCharPropOverrideEngine_FWD_DEFINED__ */


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


#ifndef __LgWritingSystemFactory_FWD_DEFINED__
#define __LgWritingSystemFactory_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgWritingSystemFactory LgWritingSystemFactory;
#else
typedef struct LgWritingSystemFactory LgWritingSystemFactory;
#endif /* __cplusplus */

#endif 	/* __LgWritingSystemFactory_FWD_DEFINED__ */


#ifndef __LgWritingSystemFactoryBuilder_FWD_DEFINED__
#define __LgWritingSystemFactoryBuilder_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgWritingSystemFactoryBuilder LgWritingSystemFactoryBuilder;
#else
typedef struct LgWritingSystemFactoryBuilder LgWritingSystemFactoryBuilder;
#endif /* __cplusplus */

#endif 	/* __LgWritingSystemFactoryBuilder_FWD_DEFINED__ */


#ifndef __LgCollation_FWD_DEFINED__
#define __LgCollation_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgCollation LgCollation;
#else
typedef struct LgCollation LgCollation;
#endif /* __cplusplus */

#endif 	/* __LgCollation_FWD_DEFINED__ */


#ifndef __LgWritingSystem_FWD_DEFINED__
#define __LgWritingSystem_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgWritingSystem LgWritingSystem;
#else
typedef struct LgWritingSystem LgWritingSystem;
#endif /* __cplusplus */

#endif 	/* __LgWritingSystem_FWD_DEFINED__ */


#ifndef __LgTsStringPlusWss_FWD_DEFINED__
#define __LgTsStringPlusWss_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgTsStringPlusWss LgTsStringPlusWss;
#else
typedef struct LgTsStringPlusWss LgTsStringPlusWss;
#endif /* __cplusplus */

#endif 	/* __LgTsStringPlusWss_FWD_DEFINED__ */


#ifndef __LgTsDataObject_FWD_DEFINED__
#define __LgTsDataObject_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgTsDataObject LgTsDataObject;
#else
typedef struct LgTsDataObject LgTsDataObject;
#endif /* __cplusplus */

#endif 	/* __LgTsDataObject_FWD_DEFINED__ */


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


#ifndef __IOleDbCommand_FWD_DEFINED__
#define __IOleDbCommand_FWD_DEFINED__
typedef interface IOleDbCommand IOleDbCommand;
#endif 	/* __IOleDbCommand_FWD_DEFINED__ */


#ifndef __IFwMetaDataCache_FWD_DEFINED__
#define __IFwMetaDataCache_FWD_DEFINED__
typedef interface IFwMetaDataCache IFwMetaDataCache;
#endif 	/* __IFwMetaDataCache_FWD_DEFINED__ */


#ifndef __IDbAdmin_FWD_DEFINED__
#define __IDbAdmin_FWD_DEFINED__
typedef interface IDbAdmin IDbAdmin;
#endif 	/* __IDbAdmin_FWD_DEFINED__ */


#ifndef __OleDbEncap_FWD_DEFINED__
#define __OleDbEncap_FWD_DEFINED__

#ifdef __cplusplus
typedef class OleDbEncap OleDbEncap;
#else
typedef struct OleDbEncap OleDbEncap;
#endif /* __cplusplus */

#endif 	/* __OleDbEncap_FWD_DEFINED__ */


#ifndef __FwMetaDataCache_FWD_DEFINED__
#define __FwMetaDataCache_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwMetaDataCache FwMetaDataCache;
#else
typedef struct FwMetaDataCache FwMetaDataCache;
#endif /* __cplusplus */

#endif 	/* __FwMetaDataCache_FWD_DEFINED__ */


#ifndef __DbAdmin_FWD_DEFINED__
#define __DbAdmin_FWD_DEFINED__

#ifdef __cplusplus
typedef class DbAdmin DbAdmin;
#else
typedef struct DbAdmin DbAdmin;
#endif /* __cplusplus */

#endif 	/* __DbAdmin_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_LanguageTlb_0000 */
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
b80ee180-c0f1-11d2-8078-0000c0fb81b5
,
LanguageLib
);


extern RPC_IF_HANDLE __MIDL_itf_LanguageTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_LanguageTlb_0000_v0_0_s_ifspec;


#ifndef __LanguageLib_LIBRARY_DEFINED__
#define __LanguageLib_LIBRARY_DEFINED__

/* library LanguageLib */
/* [helpstring][version][uuid] */





typedef /* [v1_enum] */
enum UndoResult
	{	kuresSuccess	= 0,
	kuresRefresh	= kuresSuccess + 1,
	kuresFailed	= kuresRefresh + 1,
	kuresError	= kuresFailed + 1
	} 	UndoResult;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwCustomExport
,
40300033-D5F9-4136-9A8C-B401D8582E9B
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwTool
,
37396941-4DD1-11d4-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IUndoAction
,
2F6BB7C9-1B3A-4e94-A7BF-782C2369F681
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IActionHandler
,
32C2020C-3094-42bc-80FF-45AD89826F62
);
ATTACH_GUID_TO_CLASS(class,
CDED8B0B-5BD0-43be-96C4-6B8E8E7B017D
,
ActionHandler
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IAdvInd
,
5F74AB40-EFE8-4a0d-B9AE-30F493FE6E21
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IAdvInd2
,
639C98DB-A241-496d-BE19-1EFC85CA1DD7
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IAdvInd3
,
86b6ae62-3dfa-4020-b5d1-7fa28e7726e4
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
14E389C6-C986-4e31-AE70-1CC10CC35471
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDebugReport
,
7AE7CF67-67BE-4860-8E72-AAC88294C397
);
ATTACH_GUID_TO_CLASS(class,
24636FD1-DB8D-4b2c-B4C0-44C2592CA482
,
DebugReport
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IHelpTopicProvider
,
AF8960FB-B7AF-4259-832B-38A3F5629052
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwFldSpec
,
FE44E19B-E710-4635-9690-1AFB451B1226
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IUndoGrouper
,
C38348D3-392C-4e02-BD50-A01DC4189E1D
);
ATTACH_GUID_TO_CLASS(class,
51C4C464-12D2-4CB8-96F3-66E18A6A3AC6
,
FwFldSpec
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
	knmLim	= knmNFSC + 1
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
	ktptFieldName	= 9998,
	ktptMarkItem	= 9999
	} 	FwTextPropType;

typedef /* [v1_enum] */
enum TptEditable
	{	ktptNotEditable	= 0,
	ktptIsEditable	= ktptNotEditable + 1,
	ktptSemiEditable	= ktptIsEditable + 1
	} 	TptEditable;

typedef /* [v1_enum] */
enum FwObjDataTypes
	{	kodtPictEven	= 1,
	kodtPictOdd	= 2,
	kodtNameGuidHot	= 3,
	kodtExternalPathName	= 4,
	kodtOwnNameGuidHot	= 5,
	kodtEmbeddedObjectData	= 6,
	kodtContextString	= 7,
	kodtGuidMoveableObjDisp	= 8
	} 	FwObjDataTypes;

typedef /* [v1_enum] */
enum FwTextScalarProp
	{	kscpWs	= ktptWs << 2 | 2,
	kscpWsAndOws	= ktptWs << 2 | 3,
	kscpItalic	= ktptItalic << 2 | 0,
	kscpBold	= ktptBold << 2 | 0,
	kscpSuperscript	= ktptSuperscript << 2 | 0,
	kscpUnderline	= ktptUnderline << 2 | 0,
	kscpFontSize	= ktptFontSize << 2 | 2,
	kscpOffset	= ktptOffset << 2 | 2,
	kscpForeColor	= ktptForeColor << 2 | 2,
	kscpBackColor	= ktptBackColor << 2 | 2,
	kscpUnderColor	= ktptUnderColor << 2 | 2,
	kscpBaseWs	= ktptBaseWs << 2 | 2,
	kscpBaseWsAndOws	= ktptBaseWs << 2 | 3,
	kscpAlign	= ktptAlign << 2 | 0,
	kscpFirstIndent	= ktptFirstIndent << 2 | 2,
	kscpLeadingIndent	= ktptLeadingIndent << 2 | 2,
	kscpTrailingIndent	= ktptTrailingIndent << 2 | 2,
	kscpSpaceBefore	= ktptSpaceBefore << 2 | 2,
	kscpSpaceAfter	= ktptSpaceAfter << 2 | 2,
	kscpTabDef	= ktptTabDef << 2 | 2,
	kscpLineHeight	= ktptLineHeight << 2 | 2,
	kscpParaColor	= ktptParaColor << 2 | 2,
	kscpMarkItem	= ktptMarkItem << 2 | 0
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
	kclrReserved1	= 0x80000000,
	kclrReserved2	= 0x80000001,
	kclrTransparent	= 0xc0000000
	} 	FwTextColor;

typedef /* [v1_enum] */
enum FwUnderlineType
	{	kuntMin	= 0,
	kuntNone	= kuntMin,
	kuntDotted	= kuntNone + 1,
	kuntDashed	= kuntDotted + 1,
	kuntSingle	= kuntDashed + 1,
	kuntDouble	= kuntSingle + 1,
	kuntStrikethrough	= kuntDouble + 1,
	kuntSquiggle	= kuntStrikethrough + 1,
	kuntLim	= kuntSquiggle + 1
	} 	FwUnderlineType;

typedef /* [v1_enum] */
enum FwTextAlign
	{	ktalMin	= 0,
	ktalLeading	= ktalMin,
	ktalLeft	= ktalLeading + 1,
	ktalCenter	= ktalLeft + 1,
	ktalRight	= ktalCenter + 1,
	ktalTrailing	= ktalRight + 1,
	ktalJustify	= ktalTrailing + 1,
	ktalLim	= ktalJustify + 1
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
0E9E5A6C-BA20-4245-8E26-719A67FE1892
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsTextProps
,
4FA0B99A-5A56-41A4-BE8B-B89BC62251A5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStrFactory
,
F1EF76E4-BE04-11d3-8D9A-005004DEFEC4
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
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStreamWrapper
,
4516897E-314B-49d8-8378-F2E105C80009
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
ATTACH_GUID_TO_CLASS(class,
60A7A639-3774-43e8-AE40-D911EC8E3A35
,
TsStreamWrapper
);





typedef /* [v1_enum] */
enum LgLineBreak
	{	klbNoBreak	= 0,
	klbWsBreak	= 10,
	klbWordBreak	= 15,
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
	kdmSplitPrimary	= kdmNormal + 1,
	kdmSplitSecondary	= kdmSplitPrimary + 1
	} 	LgIPDrawMode;

typedef /* [v1_enum] */
enum LgIpValidResult
	{	kipvrOK	= 0,
	kipvrBad	= kipvrOK + 1,
	kipvrUnknown	= kipvrBad + 1
	} 	LgIpValidResult;

typedef /* [v1_enum] */
enum LgTrailingWsHandling
	{	ktwshAll	= 0,
	ktwshNoWs	= ktwshAll + 1,
	ktwshOnlyWs	= ktwshNoWs + 1
	} 	LgTrailingWsHandling;

typedef /* [v1_enum] */
enum LgUtfForm
	{	kutf8	= 0,
	kutf16	= kutf8 + 1,
	kutf32	= kutf16 + 1
	} 	LgUtfForm;

typedef /* [v1_enum] */
enum VwGenericFontNames
	{	kvgfnCustom	= 0,
	kvgfnSerif	= kvgfnCustom + 1,
	kvgfnSansSerif	= kvgfnSerif + 1,
	kvgfnMonospace	= kvgfnSansSerif + 1
	} 	VwGenericFontNames;

typedef /* [v1_enum] */
enum VwFontStyle
	{	kfsNormal	= 0,
	kfsItalic	= kfsNormal + 1,
	kfsOblique	= kfsItalic + 1
	} 	VwFontStyle;

typedef /* [v1_enum] */
enum VwTextUnderline
	{	ktuNoUnderline	= 0,
	ktuSingleUnderline	= ktuNoUnderline + 1
	} 	VwTextUnderline;

typedef /* [public][public][public][public][public][public][public] */ struct __MIDL___MIDL_itf_LanguageTlb_0278_0001
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
	kjgatShrink	= kjgatStretch + 1,
	kjgatWeight	= kjgatShrink + 1,
	kjgatStep	= kjgatWeight + 1,
	kjgatChunk	= kjgatStep + 1,
	kjgatWidth	= kjgatChunk + 1,
	kjgatBreak	= kjgatWidth + 1,
	kjgatStretchInSteps	= kjgatBreak + 1,
	kjgatWidthInSteps	= kjgatStretchInSteps + 1,
	kjgatAdvWidth	= kjgatWidthInSteps + 1,
	kjgatAdvHeight	= kjgatAdvWidth + 1,
	kjgatBbLeft	= kjgatAdvHeight + 1,
	kjgatBbRight	= kjgatBbLeft + 1,
	kjgatBbTop	= kjgatBbRight + 1,
	kjgatBbBottom	= kjgatBbTop + 1
	} 	JustGlyphAttr;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_LanguageTlb_0278_0002
	{
	ScriptDirCode sdcPara;
	ScriptDirCode sdcOuter;
	} 	LgParaRenderProps;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISimpleInit
,
FC1C0D0D-0483-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwGraphics
,
3A3CE0A1-B5EB-43bd-9C89-35EAA110F12B
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwGraphicsWin32
,
8E6828A3-8681-4822-B76D-6C4A25CAECE6
);
ATTACH_GUID_TO_CLASS(class,
D9F93A03-8F8F-4e1d-B001-F373C7651B66
,
VwGraphicsWin32
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwTextSource
,
92AC8BE4-EDC8-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwJustifier
,
BAC7725F-1D26-42b2-8E9D-8B9175782CC7
);
ATTACH_GUID_TO_CLASS(class,
B424D26F-6B8C-43c2-9DD4-C4A822764472
,
VwJustifier
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSegment
,
7407F0FC-58B0-4476-A0C8-69431801E560
);
typedef /* [public][public][v1_enum] */
enum __MIDL___MIDL_itf_LanguageTlb_0284_0001
	{	kestNoMore	= 0,
	kestMoreLines	= kestNoMore + 1,
	kestHardBreak	= kestMoreLines + 1,
	kestBadBreak	= kestHardBreak + 1,
	kestOkayBreak	= kestBadBreak + 1,
	kestWsBreak	= kestOkayBreak + 1,
	kestMoreWhtsp	= kestWsBreak + 1,
	kestNothingFit	= kestMoreWhtsp + 1
	} 	LgEndSegmentType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRenderEngine
,
93CB892F-16D1-4dca-9C71-2E804BC9395C
);
ATTACH_GUID_TO_CLASS(class,
ECB3CBB1-BD20-4d85-85B4-E2ABA245933B
,
RomRenderEngine
);
ATTACH_GUID_TO_CLASS(class,
B13AAFCD-F82C-4e9e-B414-5F8EBBE48773
,
UniscribeEngine
);
ATTACH_GUID_TO_CLASS(class,
171E329C-7473-413c-959A-A8963297DA9C
,
FwGrEngine
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRenderingFeatures
,
0A439F99-7BF2-4e11-A871-8AFAEB2B7D53
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IJustifyingRenderer
,
D7364EF2-43C0-4440-872A-336A4647B9A3
);
ATTACH_GUID_TO_CLASS(class,
CFB22792-6473-4017-815C-83DF93FF43BE
,
FwGraphiteProcess
);














typedef /* [v1_enum] */
enum LgGeneralCharCategory
	{	kccLu	= 0,
	kccLl	= kccLu + 1,
	kccLt	= kccLl + 1,
	kccLm	= kccLt + 1,
	kccLo	= kccLm + 1,
	kccMn	= kccLo + 1,
	kccMc	= kccMn + 1,
	kccMe	= kccMc + 1,
	kccNd	= kccMe + 1,
	kccNl	= kccNd + 1,
	kccNo	= kccNl + 1,
	kccZs	= kccNo + 1,
	kccZl	= kccZs + 1,
	kccZp	= kccZl + 1,
	kccCc	= kccZp + 1,
	kccCf	= kccCc + 1,
	kccCs	= kccCf + 1,
	kccCo	= kccCs + 1,
	kccCn	= kccCo + 1,
	kccPc	= kccCn + 1,
	kccPd	= kccPc + 1,
	kccPs	= kccPd + 1,
	kccPe	= kccPs + 1,
	kccPi	= kccPe + 1,
	kccPf	= kccPi + 1,
	kccPo	= kccPf + 1,
	kccSm	= kccPo + 1,
	kccSc	= kccSm + 1,
	kccSk	= kccSc + 1,
	kccSo	= kccSk + 1
	} 	LgGeneralCharCategory;

typedef /* [v1_enum] */
enum LgBidiCategory
	{	kbicL	= 0,
	kbicLRE	= kbicL + 1,
	kbicLRO	= kbicLRE + 1,
	kbicR	= kbicLRO + 1,
	kbicAL	= kbicR + 1,
	kbicRLE	= kbicAL + 1,
	kbicRLO	= kbicRLE + 1,
	kbicPDF	= kbicRLO + 1,
	kbicEN	= kbicPDF + 1,
	kbicES	= kbicEN + 1,
	kbicET	= kbicES + 1,
	kbicAN	= kbicET + 1,
	kbicCS	= kbicAN + 1,
	kbicNSM	= kbicCS + 1,
	kbicBN	= kbicNSM + 1,
	kbicB	= kbicBN + 1,
	kbicS	= kbicB + 1,
	kbicWS	= kbicS + 1,
	kbicON	= kbicWS + 1
	} 	LgBidiCategory;

typedef /* [v1_enum] */
enum LgLBP
	{	klbpAI	= 0,
	klbpAL	= klbpAI + 1,
	klbpB2	= klbpAL + 1,
	klbpBA	= klbpB2 + 1,
	klbpBB	= klbpBA + 1,
	klbpBK	= klbpBB + 1,
	klbpCB	= klbpBK + 1,
	klbpCL	= klbpCB + 1,
	klbpCM	= klbpCL + 1,
	klbpCR	= klbpCM + 1,
	klbpEX	= klbpCR + 1,
	klbpGL	= klbpEX + 1,
	klbpHY	= klbpGL + 1,
	klbpID	= klbpHY + 1,
	klbpIN	= klbpID + 1,
	klbpIS	= klbpIN + 1,
	klbpLF	= klbpIS + 1,
	klbpNS	= klbpLF + 1,
	klbpNU	= klbpNS + 1,
	klbpOP	= klbpNU + 1,
	klbpPO	= klbpOP + 1,
	klbpPR	= klbpPO + 1,
	klbpQU	= klbpPR + 1,
	klbpSA	= klbpQU + 1,
	klbpSG	= klbpSA + 1,
	klbpSP	= klbpSG + 1,
	klbpSY	= klbpSP + 1,
	klbpXX	= klbpSY + 1,
	klbpZW	= klbpXX + 1
	} 	LgLBP;

typedef /* [v1_enum] */
enum LgDecompMapTag
	{	kdtNoTag	= 0,
	kdtFont	= kdtNoTag + 1,
	kdtNoBreak	= kdtFont + 1,
	kdtInitial	= kdtNoBreak + 1,
	kdtMedial	= kdtInitial + 1,
	kdtFinal	= kdtMedial + 1,
	kdtIsolated	= kdtFinal + 1,
	kdtCircle	= kdtIsolated + 1,
	kdtSuper	= kdtCircle + 1,
	kdtSub	= kdtSuper + 1,
	kdtVertical	= kdtSub + 1,
	kdtWide	= kdtVertical + 1,
	kdtNarrow	= kdtWide + 1,
	kdtSmall	= kdtNarrow + 1,
	kdtSquare	= kdtSmall + 1,
	kdtFraction	= kdtSquare + 1,
	kdtCompat	= kdtFraction + 1
	} 	LgDecompMapTag;

typedef /* [v1_enum] */
enum LgXMLTag
	{	kxmlInvalid	= 0,
	kxmlChardefs	= kxmlInvalid + 1,
	kxmlDef	= kxmlChardefs + 1,
	kxmlUdata	= kxmlDef + 1,
	kxmlLinebrk	= kxmlUdata + 1
	} 	LgXMLTag;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCollation
,
254DB9E3-0265-49CF-A19F-3C75E8525A28
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystem
,
28BC5EDC-3EF3-4db2-8B90-556200FD97ED
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgInputMethodEditor
,
17aebfe0-c00a-11d2-8078-0000c0fb81b5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgFontManager
,
10894680-F384-11d3-B5D1-00400543A266
);
ATTACH_GUID_TO_CLASS(class,
F74151C0-ABC0-11d3-BC29-00A0CC3A40C6
,
LgInputMethodEditor
);
ATTACH_GUID_TO_CLASS(class,
70553ED0-F437-11d3-B5D1-00400543A266
,
LgFontManager
);
typedef /* [v1_enum] */
enum LgCollatingOptions
	{	fcoDefault	= 0,
	fcoIgnoreCase	= 1,
	fcoDontIgnoreVariant	= 2,
	fcoLim	= fcoDontIgnoreVariant + 1
	} 	LgCollatingOptions;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCollatingEngine
,
DB78D60B-E43E-4464-B8AE-C5C9A00E2C04
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCharacterPropertyEngine
,
7C8B7F40-40C8-47f7-B10B-45372415778D
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSearchEngine
,
0D224001-03C7-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgStringConverter
,
0D224002-03C7-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTokenizer
,
0D224003-03C7-11d3-8078-0000C0FB81B5
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
0D224006-03C7-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSpellCheckFactory
,
FC1C0D01-0483-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgNumericEngine
,
FC1C0D04-0483-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystemFactory
,
2C4636E3-4F49-4966-966F-0953F97F51C8
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystemFactoryBuilder
,
8AD52AF0-13A8-4d28-A1EE-71924B36989F
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTsStringPlusWss
,
71C8D1ED-49B0-40ef-8423-92B0A5F04B89
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTsDataObject
,
56CD4356-C349-4927-9E3D-CC0CF0EFF04E
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgKeymanHandler
,
D43F4C58-5E24-4b54-8E4D-F0233B823678
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgTextServices
,
03D86B2C-9FB3-4E33-9B23-6C8BFC18FB1E
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCodePageEnumerator
,
62811E4D-5572-4f76-B71F-9F17238338E1
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgLanguageEnumerator
,
76470164-E990-411d-AF66-42A7192E4C49
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuConverterEnumerator
,
34D4E39C-C3B6-413e-9A4E-4457BBB02FE8
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuTransliteratorEnumerator
,
B26A6461-582C-4873-B3F5-673104D1AC37
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuLocaleEnumerator
,
00C88119-F57D-4e7b-A03B-EDB0BC3B57EE
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgIcuResourceBundle
,
4518189C-E545-48b4-8653-D829D1ECB778
);
ATTACH_GUID_TO_CLASS(class,
8D9C5BE3-03A8-11d3-8078-0000C0FB81B5
,
LgSystemCollater
);
ATTACH_GUID_TO_CLASS(class,
8D9C5BE4-03A8-11d3-8078-0000C0FB81B5
,
LgUnicodeCollater
);
ATTACH_GUID_TO_CLASS(class,
B076595F-EB05-4056-BF69-382B28521B10
,
LgIcuCollator
);
ATTACH_GUID_TO_CLASS(class,
FC1C0D10-0483-11d3-8078-0000C0FB81B5
,
LgCharacterPropertyEngine
);
ATTACH_GUID_TO_CLASS(class,
7CD09B42-A981-4b3b-9815-C06654CF1F7D
,
LgIcuCharPropEngine
);
ATTACH_GUID_TO_CLASS(class,
3BCA8781-182D-11d3-8078-0000C0FB81B5
,
LgCharPropOverrideEngine
);
ATTACH_GUID_TO_CLASS(class,
0D224004-03C7-11d3-8078-0000C0FB81B5
,
LgCPWordTokenizer
);
ATTACH_GUID_TO_CLASS(class,
FC1C0D03-0483-11d3-8078-0000C0FB81B5
,
LgWfiSpellChecker
);
ATTACH_GUID_TO_CLASS(class,
3BCA8782-182D-11d3-8078-0000C0FB81B5
,
LgMSWordSpellChecker
);
ATTACH_GUID_TO_CLASS(class,
FC1C0D08-0483-11d3-8078-0000C0FB81B5
,
LgNumericEngine
);
ATTACH_GUID_TO_CLASS(class,
D96B7867-EDE6-4c0d-80C6-B929300985A6
,
LgWritingSystemFactory
);
ATTACH_GUID_TO_CLASS(class,
25D66955-3AE2-4b44-A6B1-0206EA5FE264
,
LgWritingSystemFactoryBuilder
);
ATTACH_GUID_TO_CLASS(class,
CF5077EC-7582-4330-87E6-EFAE05D9FC99
,
LgCollation
);
ATTACH_GUID_TO_CLASS(class,
7EDD3897-B471-4aab-95E6-162C6DC0AC53
,
LgWritingSystem
);
ATTACH_GUID_TO_CLASS(class,
5289A9D3-E8D9-48f4-9AF7-E6014AA1E09C
,
LgTsStringPlusWss
);
ATTACH_GUID_TO_CLASS(class,
1C0BB7A2-BADB-452b-ABA3-8E60C122A670
,
LgTsDataObject
);
ATTACH_GUID_TO_CLASS(class,
740C334A-76E7-4d78-AB39-48BEAE304DEC
,
LgKeymanHandler
);
ATTACH_GUID_TO_CLASS(class,
2752634F-60F2-4065-B480-091A67C6033B
,
LgTextServices
);
ATTACH_GUID_TO_CLASS(class,
EF50E852-BA89-4014-A337-D1EF44AF0F35
,
LgCodePageEnumerator
);
ATTACH_GUID_TO_CLASS(class,
0629A66A-3877-40de-A27C-14BD51952BCF
,
LgLanguageEnumerator
);
ATTACH_GUID_TO_CLASS(class,
7583B4F0-9FA5-46df-A18B-B84DD12583CE
,
LgIcuConverterEnumerator
);
ATTACH_GUID_TO_CLASS(class,
CC9CE163-8DA6-4f6a-B387-5F77CD683434
,
LgIcuTransliteratorEnumerator
);
ATTACH_GUID_TO_CLASS(class,
38B24B19-6CAB-4745-84DF-229EA8999D24
,
LgIcuResourceBundle
);
ATTACH_GUID_TO_CLASS(class,
96C82FB7-B30A-4320-B1E7-A31951C0A30B
,
LgIcuLocaleEnumerator
);
typedef /* [v1_enum] */
enum SqlStmtType
	{	knSqlStmtNoResults	= 0,
	knSqlStmtSelectWithOneRowset	= 1,
	knSqlStmtStoredProcedure	= 2
	} 	SqlStmtType;

typedef /* [v1_enum] */
enum OdeLockTimeoutMode
	{	koltNone	= 0,
	koltMsgBox	= koltNone + 1,
	koltReturnError	= koltMsgBox + 1
	} 	OdeLockTimeoutMode;

typedef /* [v1_enum] */
enum OdeLockTimeoutValue
	{	koltvForever	= -1,
	koltvNoWait	= 0,
	koltvFwDefault	= 1000
	} 	OdeLockTimeoutValue;




GENERIC_DECLARE_SMART_INTERFACE_PTR(
IOleDbCommand
,
21993161-3E24-11d4-A1BD-00C04F0C9593
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IOleDbEncap
,
CB7BEA0F-960A-4b23-80D3-DE06C0530E04
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwMetaDataCache
,
6AA9042E-0A4D-4f33-881B-3FBE48861D14
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDbAdmin
,
2A861F95-63D0-480d-B5AF-4FAF0D22125D
);
ATTACH_GUID_TO_CLASS(class,
AAB4A4A3-3C83-11d4-A1BB-00C04F0C9593
,
OleDbEncap
);
ATTACH_GUID_TO_CLASS(class,
3A1B1AC6-24C5-4ffe-85D5-675DB4B9FCBB
,
FwMetaDataCache
);
ATTACH_GUID_TO_CLASS(class,
D584A725-8CF4-4699-941F-D1337AC7DB5C
,
DbAdmin
);

EXTERN_C const IID LIBID_LanguageLib;

#ifndef __IOleDbEncap_INTERFACE_DEFINED__
#define __IOleDbEncap_INTERFACE_DEFINED__

/* interface IOleDbEncap */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IOleDbEncap;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("CB7BEA0F-960A-4b23-80D3-DE06C0530E04")
	IOleDbEncap : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE BeginTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CommitTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CreateCommand(
			/* [out] */ IOleDbCommand **ppodc) = 0;

		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [in] */ OdeLockTimeoutMode olt,
			/* [in] */ int nmsTimeout) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsTransactionOpen(
			/* [retval][out] */ ComBool *pfTransactionOpen) = 0;

		virtual HRESULT STDMETHODCALLTYPE RollbackTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE RollbackSavePoint(
			/* [in] */ BSTR bstrSavePoint) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSavePoint(
			/* [out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSavePointOrBeginTrans(
			/* [out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitMSDE(
			/* [in] */ IStream *pfistLog,
			/* [in] */ ComBool fForce) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Server(
			/* [retval][out] */ BSTR *pbstrSvr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Database(
			/* [retval][out] */ BSTR *pbstrDb) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFreeLogKb(
			/* [in] */ int nReservespace,
			/* [out] */ int *pnKbFree) = 0;

	};

#else 	/* C style interface */

	typedef struct IOleDbEncapVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IOleDbEncap * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IOleDbEncap * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *BeginTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *CommitTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *CreateCommand )(
			IOleDbEncap * This,
			/* [out] */ IOleDbCommand **ppodc);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IOleDbEncap * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [in] */ OdeLockTimeoutMode olt,
			/* [in] */ int nmsTimeout);

		HRESULT ( STDMETHODCALLTYPE *IsTransactionOpen )(
			IOleDbEncap * This,
			/* [retval][out] */ ComBool *pfTransactionOpen);

		HRESULT ( STDMETHODCALLTYPE *RollbackTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *RollbackSavePoint )(
			IOleDbEncap * This,
			/* [in] */ BSTR bstrSavePoint);

		HRESULT ( STDMETHODCALLTYPE *SetSavePoint )(
			IOleDbEncap * This,
			/* [out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *SetSavePointOrBeginTrans )(
			IOleDbEncap * This,
			/* [out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *InitMSDE )(
			IOleDbEncap * This,
			/* [in] */ IStream *pfistLog,
			/* [in] */ ComBool fForce);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Server )(
			IOleDbEncap * This,
			/* [retval][out] */ BSTR *pbstrSvr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Database )(
			IOleDbEncap * This,
			/* [retval][out] */ BSTR *pbstrDb);

		HRESULT ( STDMETHODCALLTYPE *GetFreeLogKb )(
			IOleDbEncap * This,
			/* [in] */ int nReservespace,
			/* [out] */ int *pnKbFree);

		END_INTERFACE
	} IOleDbEncapVtbl;

	interface IOleDbEncap
	{
		CONST_VTBL struct IOleDbEncapVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IOleDbEncap_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IOleDbEncap_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IOleDbEncap_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IOleDbEncap_BeginTrans(This)	\
	(This)->lpVtbl -> BeginTrans(This)

#define IOleDbEncap_CommitTrans(This)	\
	(This)->lpVtbl -> CommitTrans(This)

#define IOleDbEncap_CreateCommand(This,ppodc)	\
	(This)->lpVtbl -> CreateCommand(This,ppodc)

#define IOleDbEncap_Init(This,bstrServer,bstrDatabase,pfistLog,olt,nmsTimeout)	\
	(This)->lpVtbl -> Init(This,bstrServer,bstrDatabase,pfistLog,olt,nmsTimeout)

#define IOleDbEncap_IsTransactionOpen(This,pfTransactionOpen)	\
	(This)->lpVtbl -> IsTransactionOpen(This,pfTransactionOpen)

#define IOleDbEncap_RollbackTrans(This)	\
	(This)->lpVtbl -> RollbackTrans(This)

#define IOleDbEncap_RollbackSavePoint(This,bstrSavePoint)	\
	(This)->lpVtbl -> RollbackSavePoint(This,bstrSavePoint)

#define IOleDbEncap_SetSavePoint(This,pbstr)	\
	(This)->lpVtbl -> SetSavePoint(This,pbstr)

#define IOleDbEncap_SetSavePointOrBeginTrans(This,pbstr)	\
	(This)->lpVtbl -> SetSavePointOrBeginTrans(This,pbstr)

#define IOleDbEncap_InitMSDE(This,pfistLog,fForce)	\
	(This)->lpVtbl -> InitMSDE(This,pfistLog,fForce)

#define IOleDbEncap_get_Server(This,pbstrSvr)	\
	(This)->lpVtbl -> get_Server(This,pbstrSvr)

#define IOleDbEncap_get_Database(This,pbstrDb)	\
	(This)->lpVtbl -> get_Database(This,pbstrDb)

#define IOleDbEncap_GetFreeLogKb(This,nReservespace,pnKbFree)	\
	(This)->lpVtbl -> GetFreeLogKb(This,nReservespace,pnKbFree)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IOleDbEncap_BeginTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_BeginTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_CommitTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_CommitTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_CreateCommand_Proxy(
	IOleDbEncap * This,
	/* [out] */ IOleDbCommand **ppodc);


void __RPC_STUB IOleDbEncap_CreateCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_Init_Proxy(
	IOleDbEncap * This,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ IStream *pfistLog,
	/* [in] */ OdeLockTimeoutMode olt,
	/* [in] */ int nmsTimeout);


void __RPC_STUB IOleDbEncap_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_IsTransactionOpen_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ ComBool *pfTransactionOpen);


void __RPC_STUB IOleDbEncap_IsTransactionOpen_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_RollbackTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_RollbackTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_RollbackSavePoint_Proxy(
	IOleDbEncap * This,
	/* [in] */ BSTR bstrSavePoint);


void __RPC_STUB IOleDbEncap_RollbackSavePoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_SetSavePoint_Proxy(
	IOleDbEncap * This,
	/* [out] */ BSTR *pbstr);


void __RPC_STUB IOleDbEncap_SetSavePoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_SetSavePointOrBeginTrans_Proxy(
	IOleDbEncap * This,
	/* [out] */ BSTR *pbstr);


void __RPC_STUB IOleDbEncap_SetSavePointOrBeginTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_InitMSDE_Proxy(
	IOleDbEncap * This,
	/* [in] */ IStream *pfistLog,
	/* [in] */ ComBool fForce);


void __RPC_STUB IOleDbEncap_InitMSDE_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IOleDbEncap_get_Server_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ BSTR *pbstrSvr);


void __RPC_STUB IOleDbEncap_get_Server_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IOleDbEncap_get_Database_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ BSTR *pbstrDb);


void __RPC_STUB IOleDbEncap_get_Database_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_GetFreeLogKb_Proxy(
	IOleDbEncap * This,
	/* [in] */ int nReservespace,
	/* [out] */ int *pnKbFree);


void __RPC_STUB IOleDbEncap_GetFreeLogKb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOleDbEncap_INTERFACE_DEFINED__ */


#ifndef __ITsString_INTERFACE_DEFINED__
#define __ITsString_INTERFACE_DEFINED__

/* interface ITsString */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsString;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0E9E5A6C-BA20-4245-8E26-719A67FE1892")
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
			/* [string][size_is][out] */ OLECHAR *prgch) = 0;

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

	};

#else 	/* C style interface */

	typedef struct ITsStringVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsString * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [string][size_is][out] */ OLECHAR *prgch);

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

		END_INTERFACE
	} ITsStringVtbl;

	interface ITsString
	{
		CONST_VTBL struct ITsStringVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ITsString_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsString_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsString_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsString_get_Text(This,pbstr)	\
	(This)->lpVtbl -> get_Text(This,pbstr)

#define ITsString_get_Length(This,pcch)	\
	(This)->lpVtbl -> get_Length(This,pcch)

#define ITsString_get_RunCount(This,pcrun)	\
	(This)->lpVtbl -> get_RunCount(This,pcrun)

#define ITsString_get_RunAt(This,ich,pirun)	\
	(This)->lpVtbl -> get_RunAt(This,ich,pirun)

#define ITsString_get_MinOfRun(This,irun,pichMin)	\
	(This)->lpVtbl -> get_MinOfRun(This,irun,pichMin)

#define ITsString_get_LimOfRun(This,irun,pichLim)	\
	(This)->lpVtbl -> get_LimOfRun(This,irun,pichLim)

#define ITsString_GetBoundsOfRun(This,irun,pichMin,pichLim)	\
	(This)->lpVtbl -> GetBoundsOfRun(This,irun,pichMin,pichLim)

#define ITsString_FetchRunInfoAt(This,ich,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfoAt(This,ich,ptri,ppttp)

#define ITsString_FetchRunInfo(This,irun,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfo(This,irun,ptri,ppttp)

#define ITsString_get_RunText(This,irun,pbstr)	\
	(This)->lpVtbl -> get_RunText(This,irun,pbstr)

#define ITsString_GetChars(This,ichMin,ichLim,pbstr)	\
	(This)->lpVtbl -> GetChars(This,ichMin,ichLim,pbstr)

#define ITsString_FetchChars(This,ichMin,ichLim,prgch)	\
	(This)->lpVtbl -> FetchChars(This,ichMin,ichLim,prgch)

#define ITsString_LockText(This,pprgch,pcch)	\
	(This)->lpVtbl -> LockText(This,pprgch,pcch)

#define ITsString_UnlockText(This,prgch)	\
	(This)->lpVtbl -> UnlockText(This,prgch)

#define ITsString_LockRun(This,irun,pprgch,pcch)	\
	(This)->lpVtbl -> LockRun(This,irun,pprgch,pcch)

#define ITsString_UnlockRun(This,irun,prgch)	\
	(This)->lpVtbl -> UnlockRun(This,irun,prgch)

#define ITsString_get_PropertiesAt(This,ich,ppttp)	\
	(This)->lpVtbl -> get_PropertiesAt(This,ich,ppttp)

#define ITsString_get_Properties(This,irun,ppttp)	\
	(This)->lpVtbl -> get_Properties(This,irun,ppttp)

#define ITsString_GetBldr(This,pptsb)	\
	(This)->lpVtbl -> GetBldr(This,pptsb)

#define ITsString_GetIncBldr(This,pptisb)	\
	(This)->lpVtbl -> GetIncBldr(This,pptisb)

#define ITsString_GetFactoryClsid(This,pclsid)	\
	(This)->lpVtbl -> GetFactoryClsid(This,pclsid)

#define ITsString_SerializeFmt(This,pstrm)	\
	(This)->lpVtbl -> SerializeFmt(This,pstrm)

#define ITsString_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	(This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)

#define ITsString_Equals(This,ptss,pfEqual)	\
	(This)->lpVtbl -> Equals(This,ptss,pfEqual)

#define ITsString_WriteAsXml(This,pstrm,pwsf,cchIndent,ws,fWriteObjData)	\
	(This)->lpVtbl -> WriteAsXml(This,pstrm,pwsf,cchIndent,ws,fWriteObjData)

#define ITsString_get_IsNormalizedForm(This,nm,pfRet)	\
	(This)->lpVtbl -> get_IsNormalizedForm(This,nm,pfRet)

#define ITsString_get_NormalizedForm(This,nm,pptssRet)	\
	(This)->lpVtbl -> get_NormalizedForm(This,nm,pptssRet)

#define ITsString_NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix)	\
	(This)->lpVtbl -> NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [id][propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_Text_Proxy(
	ITsString * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsString_get_Text_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_Length_Proxy(
	ITsString * This,
	/* [retval][out] */ int *pcch);


void __RPC_STUB ITsString_get_Length_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_RunCount_Proxy(
	ITsString * This,
	/* [retval][out] */ int *pcrun);


void __RPC_STUB ITsString_get_RunCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_RunAt_Proxy(
	ITsString * This,
	/* [in] */ int ich,
	/* [retval][out] */ int *pirun);


void __RPC_STUB ITsString_get_RunAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_MinOfRun_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [retval][out] */ int *pichMin);


void __RPC_STUB ITsString_get_MinOfRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_LimOfRun_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [retval][out] */ int *pichLim);


void __RPC_STUB ITsString_get_LimOfRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_GetBoundsOfRun_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB ITsString_GetBoundsOfRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_FetchRunInfoAt_Proxy(
	ITsString * This,
	/* [in] */ int ich,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsString_FetchRunInfoAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_FetchRunInfo_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsString_FetchRunInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_RunText_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsString_get_RunText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_GetChars_Proxy(
	ITsString * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsString_GetChars_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_FetchChars_Proxy(
	ITsString * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [string][size_is][out] */ OLECHAR *prgch);


void __RPC_STUB ITsString_FetchChars_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_LockText_Proxy(
	ITsString * This,
	/* [string][out] */ const OLECHAR **pprgch,
	/* [out] */ int *pcch);


void __RPC_STUB ITsString_LockText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_UnlockText_Proxy(
	ITsString * This,
	/* [string][in] */ const OLECHAR *prgch);


void __RPC_STUB ITsString_UnlockText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_LockRun_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [string][out] */ const OLECHAR **pprgch,
	/* [out] */ int *pcch);


void __RPC_STUB ITsString_LockRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_UnlockRun_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [string][in] */ const OLECHAR *prgch);


void __RPC_STUB ITsString_UnlockRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_PropertiesAt_Proxy(
	ITsString * This,
	/* [in] */ int ich,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsString_get_PropertiesAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_Properties_Proxy(
	ITsString * This,
	/* [in] */ int irun,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsString_get_Properties_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_GetBldr_Proxy(
	ITsString * This,
	/* [retval][out] */ ITsStrBldr **pptsb);


void __RPC_STUB ITsString_GetBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_GetIncBldr_Proxy(
	ITsString * This,
	/* [retval][out] */ ITsIncStrBldr **pptisb);


void __RPC_STUB ITsString_GetIncBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_GetFactoryClsid_Proxy(
	ITsString * This,
	/* [retval][out] */ CLSID *pclsid);


void __RPC_STUB ITsString_GetFactoryClsid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_SerializeFmt_Proxy(
	ITsString * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB ITsString_SerializeFmt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_SerializeFmtRgb_Proxy(
	ITsString * This,
	/* [size_is][out] */ BYTE *prgb,
	/* [in] */ int cbMax,
	/* [retval][out] */ int *pcbNeeded);


void __RPC_STUB ITsString_SerializeFmtRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_Equals_Proxy(
	ITsString * This,
	/* [in] */ ITsString *ptss,
	/* [retval][out] */ ComBool *pfEqual);


void __RPC_STUB ITsString_Equals_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsString_WriteAsXml_Proxy(
	ITsString * This,
	/* [in] */ IStream *pstrm,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [in] */ int cchIndent,
	/* [in] */ int ws,
	/* [in] */ ComBool fWriteObjData);


void __RPC_STUB ITsString_WriteAsXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_IsNormalizedForm_Proxy(
	ITsString * This,
	/* [in] */ FwNormalizationMode nm,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ITsString_get_IsNormalizedForm_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsString_get_NormalizedForm_Proxy(
	ITsString * This,
	/* [in] */ FwNormalizationMode nm,
	/* [retval][out] */ ITsString **pptssRet);


void __RPC_STUB ITsString_get_NormalizedForm_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsString_NfdAndFixOffsets_Proxy(
	ITsString * This,
	/* [out] */ ITsString **pptssRet,
	/* [size_is][in] */ int **prgpichOffsetsToFix,
	/* [in] */ int cichOffsetsToFix);


void __RPC_STUB ITsString_NfdAndFixOffsets_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsString_INTERFACE_DEFINED__ */


#ifndef __IFwFldSpec_INTERFACE_DEFINED__
#define __IFwFldSpec_INTERFACE_DEFINED__

/* interface IFwFldSpec */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwFldSpec;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FE44E19B-E710-4635-9690-1AFB451B1226")
	IFwFldSpec : public IUnknown
	{
	public:
		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Visibility(
			/* [in] */ int nVis) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Visibility(
			/* [retval][out] */ int *pnVis) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_HideLabel(
			/* [in] */ ComBool fHide) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HideLabel(
			/* [retval][out] */ ComBool *pfHide) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Label(
			/* [in] */ ITsString *ptssLabel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Label(
			/* [retval][out] */ ITsString **pptssLabel) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FieldId(
			/* [in] */ int flid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldId(
			/* [retval][out] */ int *pflid) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ClassName(
			/* [in] */ BSTR bstrClsName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassName(
			/* [retval][out] */ BSTR *pbstrClsName) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FieldName(
			/* [in] */ BSTR bstrFieldName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldName(
			/* [retval][out] */ BSTR *pbstrFieldName) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Style(
			/* [in] */ BSTR bstrStyle) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Style(
			/* [retval][out] */ BSTR *pbstrStyle) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwFldSpecVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwFldSpec * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwFldSpec * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwFldSpec * This);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Visibility )(
			IFwFldSpec * This,
			/* [in] */ int nVis);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Visibility )(
			IFwFldSpec * This,
			/* [retval][out] */ int *pnVis);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_HideLabel )(
			IFwFldSpec * This,
			/* [in] */ ComBool fHide);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HideLabel )(
			IFwFldSpec * This,
			/* [retval][out] */ ComBool *pfHide);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Label )(
			IFwFldSpec * This,
			/* [in] */ ITsString *ptssLabel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Label )(
			IFwFldSpec * This,
			/* [retval][out] */ ITsString **pptssLabel);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FieldId )(
			IFwFldSpec * This,
			/* [in] */ int flid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldId )(
			IFwFldSpec * This,
			/* [retval][out] */ int *pflid);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ClassName )(
			IFwFldSpec * This,
			/* [in] */ BSTR bstrClsName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassName )(
			IFwFldSpec * This,
			/* [retval][out] */ BSTR *pbstrClsName);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FieldName )(
			IFwFldSpec * This,
			/* [in] */ BSTR bstrFieldName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldName )(
			IFwFldSpec * This,
			/* [retval][out] */ BSTR *pbstrFieldName);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Style )(
			IFwFldSpec * This,
			/* [in] */ BSTR bstrStyle);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Style )(
			IFwFldSpec * This,
			/* [retval][out] */ BSTR *pbstrStyle);

		END_INTERFACE
	} IFwFldSpecVtbl;

	interface IFwFldSpec
	{
		CONST_VTBL struct IFwFldSpecVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwFldSpec_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwFldSpec_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwFldSpec_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwFldSpec_put_Visibility(This,nVis)	\
	(This)->lpVtbl -> put_Visibility(This,nVis)

#define IFwFldSpec_get_Visibility(This,pnVis)	\
	(This)->lpVtbl -> get_Visibility(This,pnVis)

#define IFwFldSpec_put_HideLabel(This,fHide)	\
	(This)->lpVtbl -> put_HideLabel(This,fHide)

#define IFwFldSpec_get_HideLabel(This,pfHide)	\
	(This)->lpVtbl -> get_HideLabel(This,pfHide)

#define IFwFldSpec_put_Label(This,ptssLabel)	\
	(This)->lpVtbl -> put_Label(This,ptssLabel)

#define IFwFldSpec_get_Label(This,pptssLabel)	\
	(This)->lpVtbl -> get_Label(This,pptssLabel)

#define IFwFldSpec_put_FieldId(This,flid)	\
	(This)->lpVtbl -> put_FieldId(This,flid)

#define IFwFldSpec_get_FieldId(This,pflid)	\
	(This)->lpVtbl -> get_FieldId(This,pflid)

#define IFwFldSpec_put_ClassName(This,bstrClsName)	\
	(This)->lpVtbl -> put_ClassName(This,bstrClsName)

#define IFwFldSpec_get_ClassName(This,pbstrClsName)	\
	(This)->lpVtbl -> get_ClassName(This,pbstrClsName)

#define IFwFldSpec_put_FieldName(This,bstrFieldName)	\
	(This)->lpVtbl -> put_FieldName(This,bstrFieldName)

#define IFwFldSpec_get_FieldName(This,pbstrFieldName)	\
	(This)->lpVtbl -> get_FieldName(This,pbstrFieldName)

#define IFwFldSpec_put_Style(This,bstrStyle)	\
	(This)->lpVtbl -> put_Style(This,bstrStyle)

#define IFwFldSpec_get_Style(This,pbstrStyle)	\
	(This)->lpVtbl -> get_Style(This,pbstrStyle)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_Visibility_Proxy(
	IFwFldSpec * This,
	/* [in] */ int nVis);


void __RPC_STUB IFwFldSpec_put_Visibility_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_Visibility_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ int *pnVis);


void __RPC_STUB IFwFldSpec_get_Visibility_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_HideLabel_Proxy(
	IFwFldSpec * This,
	/* [in] */ ComBool fHide);


void __RPC_STUB IFwFldSpec_put_HideLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_HideLabel_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ ComBool *pfHide);


void __RPC_STUB IFwFldSpec_get_HideLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_Label_Proxy(
	IFwFldSpec * This,
	/* [in] */ ITsString *ptssLabel);


void __RPC_STUB IFwFldSpec_put_Label_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_Label_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ ITsString **pptssLabel);


void __RPC_STUB IFwFldSpec_get_Label_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_FieldId_Proxy(
	IFwFldSpec * This,
	/* [in] */ int flid);


void __RPC_STUB IFwFldSpec_put_FieldId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_FieldId_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ int *pflid);


void __RPC_STUB IFwFldSpec_get_FieldId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_ClassName_Proxy(
	IFwFldSpec * This,
	/* [in] */ BSTR bstrClsName);


void __RPC_STUB IFwFldSpec_put_ClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_ClassName_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ BSTR *pbstrClsName);


void __RPC_STUB IFwFldSpec_get_ClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_FieldName_Proxy(
	IFwFldSpec * This,
	/* [in] */ BSTR bstrFieldName);


void __RPC_STUB IFwFldSpec_put_FieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_FieldName_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ BSTR *pbstrFieldName);


void __RPC_STUB IFwFldSpec_get_FieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_put_Style_Proxy(
	IFwFldSpec * This,
	/* [in] */ BSTR bstrStyle);


void __RPC_STUB IFwFldSpec_put_Style_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwFldSpec_get_Style_Proxy(
	IFwFldSpec * This,
	/* [retval][out] */ BSTR *pbstrStyle);


void __RPC_STUB IFwFldSpec_get_Style_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwFldSpec_INTERFACE_DEFINED__ */


#ifndef __IUndoGrouper_INTERFACE_DEFINED__
#define __IUndoGrouper_INTERFACE_DEFINED__

/* interface IUndoGrouper */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IUndoGrouper;

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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IUndoGrouper_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IUndoGrouper_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IUndoGrouper_BeginGroup(This,phndl)	\
	(This)->lpVtbl -> BeginGroup(This,phndl)

#define IUndoGrouper_EndGroup(This,hndl)	\
	(This)->lpVtbl -> EndGroup(This,hndl)

#define IUndoGrouper_CancelGroup(This,hndl)	\
	(This)->lpVtbl -> CancelGroup(This,hndl)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IUndoGrouper_BeginGroup_Proxy(
	IUndoGrouper * This,
	/* [retval][out] */ int *phndl);


void __RPC_STUB IUndoGrouper_BeginGroup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IUndoGrouper_EndGroup_Proxy(
	IUndoGrouper * This,
	/* [in] */ int hndl);


void __RPC_STUB IUndoGrouper_EndGroup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IUndoGrouper_CancelGroup_Proxy(
	IUndoGrouper * This,
	/* [in] */ int hndl);


void __RPC_STUB IUndoGrouper_CancelGroup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IUndoGrouper_INTERFACE_DEFINED__ */


#ifndef __IFwCustomExport_INTERFACE_DEFINED__
#define __IFwCustomExport_INTERFACE_DEFINED__

/* interface IFwCustomExport */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwCustomExport;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("40300033-D5F9-4136-9A8C-B401D8582E9B")
	IFwCustomExport : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetLabelStyles(
			/* [in] */ BSTR bstrLabel,
			/* [in] */ BSTR bstrSubLabel) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddFlidCharStyleMapping(
			/* [in] */ int flid,
			/* [in] */ BSTR bstrStyle) = 0;

		virtual HRESULT STDMETHODCALLTYPE BuildSubItemsString(
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE BuildObjRefSeqString(
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE BuildObjRefAtomicString(
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE BuildExpandableString(
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetEnumString(
			/* [in] */ int flid,
			/* [in] */ int itss,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetActualLevel(
			/* [in] */ int nLevel,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ int *pnActualLevel) = 0;

		virtual HRESULT STDMETHODCALLTYPE BuildRecordTags(
			/* [in] */ int nLevel,
			/* [in] */ int hvo,
			/* [in] */ int clid,
			/* [out] */ BSTR *pbstrStartTag,
			/* [out] */ BSTR *pbstrEndTag) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetPageSetupInfo(
			/* [out] */ int *pnOrientation,
			/* [out] */ int *pnPaperSize,
			/* [out] */ int *pdxmpLeftMargin,
			/* [out] */ int *pdxmpRightMargin,
			/* [out] */ int *pdympTopMargin,
			/* [out] */ int *pdympBottomMargin,
			/* [out] */ int *pdympHeaderMargin,
			/* [out] */ int *pdympFooterMargin,
			/* [out] */ int *pdxmpPageWidth,
			/* [out] */ int *pdympPageHeight,
			/* [out] */ ITsString **pptssHeader,
			/* [out] */ ITsString **pptssFooter) = 0;

		virtual HRESULT STDMETHODCALLTYPE PostProcessFile(
			/* [in] */ BSTR bstrInputFile,
			/* [retval][out] */ BSTR *pbstrOutputFile) = 0;

		virtual HRESULT STDMETHODCALLTYPE IncludeObjectData(
			/* [retval][out] */ ComBool *pbWriteObjData) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwCustomExportVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwCustomExport * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwCustomExport * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwCustomExport * This);

		HRESULT ( STDMETHODCALLTYPE *SetLabelStyles )(
			IFwCustomExport * This,
			/* [in] */ BSTR bstrLabel,
			/* [in] */ BSTR bstrSubLabel);

		HRESULT ( STDMETHODCALLTYPE *AddFlidCharStyleMapping )(
			IFwCustomExport * This,
			/* [in] */ int flid,
			/* [in] */ BSTR bstrStyle);

		HRESULT ( STDMETHODCALLTYPE *BuildSubItemsString )(
			IFwCustomExport * This,
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *BuildObjRefSeqString )(
			IFwCustomExport * This,
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *BuildObjRefAtomicString )(
			IFwCustomExport * This,
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *BuildExpandableString )(
			IFwCustomExport * This,
			/* [in] */ IFwFldSpec *pffsp,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *GetEnumString )(
			IFwCustomExport * This,
			/* [in] */ int flid,
			/* [in] */ int itss,
			/* [retval][out] */ BSTR *pbstrName);

		HRESULT ( STDMETHODCALLTYPE *GetActualLevel )(
			IFwCustomExport * This,
			/* [in] */ int nLevel,
			/* [in] */ int hvoRec,
			/* [in] */ int ws,
			/* [retval][out] */ int *pnActualLevel);

		HRESULT ( STDMETHODCALLTYPE *BuildRecordTags )(
			IFwCustomExport * This,
			/* [in] */ int nLevel,
			/* [in] */ int hvo,
			/* [in] */ int clid,
			/* [out] */ BSTR *pbstrStartTag,
			/* [out] */ BSTR *pbstrEndTag);

		HRESULT ( STDMETHODCALLTYPE *GetPageSetupInfo )(
			IFwCustomExport * This,
			/* [out] */ int *pnOrientation,
			/* [out] */ int *pnPaperSize,
			/* [out] */ int *pdxmpLeftMargin,
			/* [out] */ int *pdxmpRightMargin,
			/* [out] */ int *pdympTopMargin,
			/* [out] */ int *pdympBottomMargin,
			/* [out] */ int *pdympHeaderMargin,
			/* [out] */ int *pdympFooterMargin,
			/* [out] */ int *pdxmpPageWidth,
			/* [out] */ int *pdympPageHeight,
			/* [out] */ ITsString **pptssHeader,
			/* [out] */ ITsString **pptssFooter);

		HRESULT ( STDMETHODCALLTYPE *PostProcessFile )(
			IFwCustomExport * This,
			/* [in] */ BSTR bstrInputFile,
			/* [retval][out] */ BSTR *pbstrOutputFile);

		HRESULT ( STDMETHODCALLTYPE *IncludeObjectData )(
			IFwCustomExport * This,
			/* [retval][out] */ ComBool *pbWriteObjData);

		END_INTERFACE
	} IFwCustomExportVtbl;

	interface IFwCustomExport
	{
		CONST_VTBL struct IFwCustomExportVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwCustomExport_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwCustomExport_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwCustomExport_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwCustomExport_SetLabelStyles(This,bstrLabel,bstrSubLabel)	\
	(This)->lpVtbl -> SetLabelStyles(This,bstrLabel,bstrSubLabel)

#define IFwCustomExport_AddFlidCharStyleMapping(This,flid,bstrStyle)	\
	(This)->lpVtbl -> AddFlidCharStyleMapping(This,flid,bstrStyle)

#define IFwCustomExport_BuildSubItemsString(This,pffsp,hvoRec,ws,pptss)	\
	(This)->lpVtbl -> BuildSubItemsString(This,pffsp,hvoRec,ws,pptss)

#define IFwCustomExport_BuildObjRefSeqString(This,pffsp,hvoRec,ws,pptss)	\
	(This)->lpVtbl -> BuildObjRefSeqString(This,pffsp,hvoRec,ws,pptss)

#define IFwCustomExport_BuildObjRefAtomicString(This,pffsp,hvoRec,ws,pptss)	\
	(This)->lpVtbl -> BuildObjRefAtomicString(This,pffsp,hvoRec,ws,pptss)

#define IFwCustomExport_BuildExpandableString(This,pffsp,hvoRec,ws,pptss)	\
	(This)->lpVtbl -> BuildExpandableString(This,pffsp,hvoRec,ws,pptss)

#define IFwCustomExport_GetEnumString(This,flid,itss,pbstrName)	\
	(This)->lpVtbl -> GetEnumString(This,flid,itss,pbstrName)

#define IFwCustomExport_GetActualLevel(This,nLevel,hvoRec,ws,pnActualLevel)	\
	(This)->lpVtbl -> GetActualLevel(This,nLevel,hvoRec,ws,pnActualLevel)

#define IFwCustomExport_BuildRecordTags(This,nLevel,hvo,clid,pbstrStartTag,pbstrEndTag)	\
	(This)->lpVtbl -> BuildRecordTags(This,nLevel,hvo,clid,pbstrStartTag,pbstrEndTag)

#define IFwCustomExport_GetPageSetupInfo(This,pnOrientation,pnPaperSize,pdxmpLeftMargin,pdxmpRightMargin,pdympTopMargin,pdympBottomMargin,pdympHeaderMargin,pdympFooterMargin,pdxmpPageWidth,pdympPageHeight,pptssHeader,pptssFooter)	\
	(This)->lpVtbl -> GetPageSetupInfo(This,pnOrientation,pnPaperSize,pdxmpLeftMargin,pdxmpRightMargin,pdympTopMargin,pdympBottomMargin,pdympHeaderMargin,pdympFooterMargin,pdxmpPageWidth,pdympPageHeight,pptssHeader,pptssFooter)

#define IFwCustomExport_PostProcessFile(This,bstrInputFile,pbstrOutputFile)	\
	(This)->lpVtbl -> PostProcessFile(This,bstrInputFile,pbstrOutputFile)

#define IFwCustomExport_IncludeObjectData(This,pbWriteObjData)	\
	(This)->lpVtbl -> IncludeObjectData(This,pbWriteObjData)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwCustomExport_SetLabelStyles_Proxy(
	IFwCustomExport * This,
	/* [in] */ BSTR bstrLabel,
	/* [in] */ BSTR bstrSubLabel);


void __RPC_STUB IFwCustomExport_SetLabelStyles_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_AddFlidCharStyleMapping_Proxy(
	IFwCustomExport * This,
	/* [in] */ int flid,
	/* [in] */ BSTR bstrStyle);


void __RPC_STUB IFwCustomExport_AddFlidCharStyleMapping_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_BuildSubItemsString_Proxy(
	IFwCustomExport * This,
	/* [in] */ IFwFldSpec *pffsp,
	/* [in] */ int hvoRec,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB IFwCustomExport_BuildSubItemsString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_BuildObjRefSeqString_Proxy(
	IFwCustomExport * This,
	/* [in] */ IFwFldSpec *pffsp,
	/* [in] */ int hvoRec,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB IFwCustomExport_BuildObjRefSeqString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_BuildObjRefAtomicString_Proxy(
	IFwCustomExport * This,
	/* [in] */ IFwFldSpec *pffsp,
	/* [in] */ int hvoRec,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB IFwCustomExport_BuildObjRefAtomicString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_BuildExpandableString_Proxy(
	IFwCustomExport * This,
	/* [in] */ IFwFldSpec *pffsp,
	/* [in] */ int hvoRec,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB IFwCustomExport_BuildExpandableString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_GetEnumString_Proxy(
	IFwCustomExport * This,
	/* [in] */ int flid,
	/* [in] */ int itss,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB IFwCustomExport_GetEnumString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_GetActualLevel_Proxy(
	IFwCustomExport * This,
	/* [in] */ int nLevel,
	/* [in] */ int hvoRec,
	/* [in] */ int ws,
	/* [retval][out] */ int *pnActualLevel);


void __RPC_STUB IFwCustomExport_GetActualLevel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_BuildRecordTags_Proxy(
	IFwCustomExport * This,
	/* [in] */ int nLevel,
	/* [in] */ int hvo,
	/* [in] */ int clid,
	/* [out] */ BSTR *pbstrStartTag,
	/* [out] */ BSTR *pbstrEndTag);


void __RPC_STUB IFwCustomExport_BuildRecordTags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_GetPageSetupInfo_Proxy(
	IFwCustomExport * This,
	/* [out] */ int *pnOrientation,
	/* [out] */ int *pnPaperSize,
	/* [out] */ int *pdxmpLeftMargin,
	/* [out] */ int *pdxmpRightMargin,
	/* [out] */ int *pdympTopMargin,
	/* [out] */ int *pdympBottomMargin,
	/* [out] */ int *pdympHeaderMargin,
	/* [out] */ int *pdympFooterMargin,
	/* [out] */ int *pdxmpPageWidth,
	/* [out] */ int *pdympPageHeight,
	/* [out] */ ITsString **pptssHeader,
	/* [out] */ ITsString **pptssFooter);


void __RPC_STUB IFwCustomExport_GetPageSetupInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_PostProcessFile_Proxy(
	IFwCustomExport * This,
	/* [in] */ BSTR bstrInputFile,
	/* [retval][out] */ BSTR *pbstrOutputFile);


void __RPC_STUB IFwCustomExport_PostProcessFile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwCustomExport_IncludeObjectData_Proxy(
	IFwCustomExport * This,
	/* [retval][out] */ ComBool *pbWriteObjData);


void __RPC_STUB IFwCustomExport_IncludeObjectData_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwCustomExport_INTERFACE_DEFINED__ */


#ifndef __IFwTool_INTERFACE_DEFINED__
#define __IFwTool_INTERFACE_DEFINED__

/* interface IFwTool */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwTool;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("37396941-4DD1-11d4-8078-0000C0FB81B5")
	IFwTool : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE NewMainWnd(
			/* [in] */ BSTR bstrServerName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int hvoLangProj,
			/* [in] */ int hvoMainObj,
			/* [in] */ int encUi,
			/* [in] */ int nTool,
			/* [in] */ int nParam,
			/* [out] */ int *ppidNew,
			/* [retval][out] */ long *phtool) = 0;

		virtual HRESULT STDMETHODCALLTYPE NewMainWndWithSel(
			/* [in] */ BSTR bstrServerName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int hvoLangProj,
			/* [in] */ int hvoMainObj,
			/* [in] */ int encUi,
			/* [in] */ int nTool,
			/* [in] */ int nParam,
			/* [size_is][in] */ const long *prghvo,
			/* [in] */ int chvo,
			/* [size_is][in] */ const int *prgflid,
			/* [in] */ int cflid,
			/* [in] */ int ichCur,
			/* [in] */ int nView,
			/* [out] */ int *ppidNew,
			/* [retval][out] */ long *phtool) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseMainWnd(
			/* [in] */ long htool,
			/* [retval][out] */ ComBool *pfCancelled) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseDbAndWindows(
			/* [in] */ BSTR bstrSvrName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ ComBool fOkToClose) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwToolVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwTool * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwTool * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwTool * This);

		HRESULT ( STDMETHODCALLTYPE *NewMainWnd )(
			IFwTool * This,
			/* [in] */ BSTR bstrServerName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int hvoLangProj,
			/* [in] */ int hvoMainObj,
			/* [in] */ int encUi,
			/* [in] */ int nTool,
			/* [in] */ int nParam,
			/* [out] */ int *ppidNew,
			/* [retval][out] */ long *phtool);

		HRESULT ( STDMETHODCALLTYPE *NewMainWndWithSel )(
			IFwTool * This,
			/* [in] */ BSTR bstrServerName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int hvoLangProj,
			/* [in] */ int hvoMainObj,
			/* [in] */ int encUi,
			/* [in] */ int nTool,
			/* [in] */ int nParam,
			/* [size_is][in] */ const long *prghvo,
			/* [in] */ int chvo,
			/* [size_is][in] */ const int *prgflid,
			/* [in] */ int cflid,
			/* [in] */ int ichCur,
			/* [in] */ int nView,
			/* [out] */ int *ppidNew,
			/* [retval][out] */ long *phtool);

		HRESULT ( STDMETHODCALLTYPE *CloseMainWnd )(
			IFwTool * This,
			/* [in] */ long htool,
			/* [retval][out] */ ComBool *pfCancelled);

		HRESULT ( STDMETHODCALLTYPE *CloseDbAndWindows )(
			IFwTool * This,
			/* [in] */ BSTR bstrSvrName,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ ComBool fOkToClose);

		END_INTERFACE
	} IFwToolVtbl;

	interface IFwTool
	{
		CONST_VTBL struct IFwToolVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwTool_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwTool_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwTool_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwTool_NewMainWnd(This,bstrServerName,bstrDbName,hvoLangProj,hvoMainObj,encUi,nTool,nParam,ppidNew,phtool)	\
	(This)->lpVtbl -> NewMainWnd(This,bstrServerName,bstrDbName,hvoLangProj,hvoMainObj,encUi,nTool,nParam,ppidNew,phtool)

#define IFwTool_NewMainWndWithSel(This,bstrServerName,bstrDbName,hvoLangProj,hvoMainObj,encUi,nTool,nParam,prghvo,chvo,prgflid,cflid,ichCur,nView,ppidNew,phtool)	\
	(This)->lpVtbl -> NewMainWndWithSel(This,bstrServerName,bstrDbName,hvoLangProj,hvoMainObj,encUi,nTool,nParam,prghvo,chvo,prgflid,cflid,ichCur,nView,ppidNew,phtool)

#define IFwTool_CloseMainWnd(This,htool,pfCancelled)	\
	(This)->lpVtbl -> CloseMainWnd(This,htool,pfCancelled)

#define IFwTool_CloseDbAndWindows(This,bstrSvrName,bstrDbName,fOkToClose)	\
	(This)->lpVtbl -> CloseDbAndWindows(This,bstrSvrName,bstrDbName,fOkToClose)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwTool_NewMainWnd_Proxy(
	IFwTool * This,
	/* [in] */ BSTR bstrServerName,
	/* [in] */ BSTR bstrDbName,
	/* [in] */ int hvoLangProj,
	/* [in] */ int hvoMainObj,
	/* [in] */ int encUi,
	/* [in] */ int nTool,
	/* [in] */ int nParam,
	/* [out] */ int *ppidNew,
	/* [retval][out] */ long *phtool);


void __RPC_STUB IFwTool_NewMainWnd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwTool_NewMainWndWithSel_Proxy(
	IFwTool * This,
	/* [in] */ BSTR bstrServerName,
	/* [in] */ BSTR bstrDbName,
	/* [in] */ int hvoLangProj,
	/* [in] */ int hvoMainObj,
	/* [in] */ int encUi,
	/* [in] */ int nTool,
	/* [in] */ int nParam,
	/* [size_is][in] */ const long *prghvo,
	/* [in] */ int chvo,
	/* [size_is][in] */ const int *prgflid,
	/* [in] */ int cflid,
	/* [in] */ int ichCur,
	/* [in] */ int nView,
	/* [out] */ int *ppidNew,
	/* [retval][out] */ long *phtool);


void __RPC_STUB IFwTool_NewMainWndWithSel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwTool_CloseMainWnd_Proxy(
	IFwTool * This,
	/* [in] */ long htool,
	/* [retval][out] */ ComBool *pfCancelled);


void __RPC_STUB IFwTool_CloseMainWnd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwTool_CloseDbAndWindows_Proxy(
	IFwTool * This,
	/* [in] */ BSTR bstrSvrName,
	/* [in] */ BSTR bstrDbName,
	/* [in] */ ComBool fOkToClose);


void __RPC_STUB IFwTool_CloseDbAndWindows_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwTool_INTERFACE_DEFINED__ */


#ifndef __IUndoAction_INTERFACE_DEFINED__
#define __IUndoAction_INTERFACE_DEFINED__

/* interface IUndoAction */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IUndoAction;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2F6BB7C9-1B3A-4e94-A7BF-782C2369F681")
	IUndoAction : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Undo(
			/* [retval][out] */ ComBool *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE Redo(
			/* [retval][out] */ ComBool *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE Commit( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsDataChange(
			/* [retval][out] */ ComBool *pfRet) = 0;

	};

#else 	/* C style interface */

	typedef struct IUndoActionVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IUndoAction * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *IsDataChange )(
			IUndoAction * This,
			/* [retval][out] */ ComBool *pfRet);

		END_INTERFACE
	} IUndoActionVtbl;

	interface IUndoAction
	{
		CONST_VTBL struct IUndoActionVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IUndoAction_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IUndoAction_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IUndoAction_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IUndoAction_Undo(This,pfSuccess)	\
	(This)->lpVtbl -> Undo(This,pfSuccess)

#define IUndoAction_Redo(This,pfSuccess)	\
	(This)->lpVtbl -> Redo(This,pfSuccess)

#define IUndoAction_Commit(This)	\
	(This)->lpVtbl -> Commit(This)

#define IUndoAction_IsDataChange(This,pfRet)	\
	(This)->lpVtbl -> IsDataChange(This,pfRet)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IUndoAction_Undo_Proxy(
	IUndoAction * This,
	/* [retval][out] */ ComBool *pfSuccess);


void __RPC_STUB IUndoAction_Undo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IUndoAction_Redo_Proxy(
	IUndoAction * This,
	/* [retval][out] */ ComBool *pfSuccess);


void __RPC_STUB IUndoAction_Redo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IUndoAction_Commit_Proxy(
	IUndoAction * This);


void __RPC_STUB IUndoAction_Commit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IUndoAction_IsDataChange_Proxy(
	IUndoAction * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IUndoAction_IsDataChange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IUndoAction_INTERFACE_DEFINED__ */


#ifndef __IActionHandler_INTERFACE_DEFINED__
#define __IActionHandler_INTERFACE_DEFINED__

/* interface IActionHandler */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IActionHandler;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("32C2020C-3094-42bc-80FF-45AD89826F62")
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

		virtual HRESULT STDMETHODCALLTYPE Commit( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Mark(
			/* [retval][out] */ int *phMark) = 0;

		virtual HRESULT STDMETHODCALLTYPE CollapseToMark(
			/* [in] */ int hMark,
			/* [in] */ BSTR bstrUndo,
			/* [in] */ BSTR bstrRedo) = 0;

		virtual HRESULT STDMETHODCALLTYPE DiscardToMark(
			/* [in] */ int hMark) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TopMarkHandle(
			/* [retval][out] */ int *phMark) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TasksSinceMark(
			/* [in] */ ComBool fUndo,
			/* [retval][out] */ ComBool *pf) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoableActionCount(
			/* [retval][out] */ int *pcSeq) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoableSequenceCount(
			/* [retval][out] */ int *pcSeq) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RedoableSequenceCount(
			/* [retval][out] */ int *pcSeq) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoGrouper(
			/* [retval][out] */ IUndoGrouper **ppundg) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UndoGrouper(
			/* [in] */ IUndoGrouper *pundg) = 0;

	};

#else 	/* C style interface */

	typedef struct IActionHandlerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IActionHandler * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [in] */ BSTR bstrRedo);

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
			/* [retval][out] */ int *pcSeq);

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

		END_INTERFACE
	} IActionHandlerVtbl;

	interface IActionHandler
	{
		CONST_VTBL struct IActionHandlerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IActionHandler_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IActionHandler_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IActionHandler_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IActionHandler_BeginUndoTask(This,bstrUndo,bstrRedo)	\
	(This)->lpVtbl -> BeginUndoTask(This,bstrUndo,bstrRedo)

#define IActionHandler_EndUndoTask(This)	\
	(This)->lpVtbl -> EndUndoTask(This)

#define IActionHandler_ContinueUndoTask(This)	\
	(This)->lpVtbl -> ContinueUndoTask(This)

#define IActionHandler_EndOuterUndoTask(This)	\
	(This)->lpVtbl -> EndOuterUndoTask(This)

#define IActionHandler_BreakUndoTask(This,bstrUndo,bstrRedo)	\
	(This)->lpVtbl -> BreakUndoTask(This,bstrUndo,bstrRedo)

#define IActionHandler_StartSeq(This,bstrUndo,bstrRedo,puact)	\
	(This)->lpVtbl -> StartSeq(This,bstrUndo,bstrRedo,puact)

#define IActionHandler_AddAction(This,puact)	\
	(This)->lpVtbl -> AddAction(This,puact)

#define IActionHandler_GetUndoText(This,pbstrUndoText)	\
	(This)->lpVtbl -> GetUndoText(This,pbstrUndoText)

#define IActionHandler_GetUndoTextN(This,iAct,pbstrUndoText)	\
	(This)->lpVtbl -> GetUndoTextN(This,iAct,pbstrUndoText)

#define IActionHandler_GetRedoText(This,pbstrRedoText)	\
	(This)->lpVtbl -> GetRedoText(This,pbstrRedoText)

#define IActionHandler_GetRedoTextN(This,iAct,pbstrRedoText)	\
	(This)->lpVtbl -> GetRedoTextN(This,iAct,pbstrRedoText)

#define IActionHandler_CanUndo(This,pfCanUndo)	\
	(This)->lpVtbl -> CanUndo(This,pfCanUndo)

#define IActionHandler_CanRedo(This,pfCanRedo)	\
	(This)->lpVtbl -> CanRedo(This,pfCanRedo)

#define IActionHandler_Undo(This,pures)	\
	(This)->lpVtbl -> Undo(This,pures)

#define IActionHandler_Redo(This,pures)	\
	(This)->lpVtbl -> Redo(This,pures)

#define IActionHandler_Commit(This)	\
	(This)->lpVtbl -> Commit(This)

#define IActionHandler_Close(This)	\
	(This)->lpVtbl -> Close(This)

#define IActionHandler_Mark(This,phMark)	\
	(This)->lpVtbl -> Mark(This,phMark)

#define IActionHandler_CollapseToMark(This,hMark,bstrUndo,bstrRedo)	\
	(This)->lpVtbl -> CollapseToMark(This,hMark,bstrUndo,bstrRedo)

#define IActionHandler_DiscardToMark(This,hMark)	\
	(This)->lpVtbl -> DiscardToMark(This,hMark)

#define IActionHandler_get_TopMarkHandle(This,phMark)	\
	(This)->lpVtbl -> get_TopMarkHandle(This,phMark)

#define IActionHandler_get_TasksSinceMark(This,fUndo,pf)	\
	(This)->lpVtbl -> get_TasksSinceMark(This,fUndo,pf)

#define IActionHandler_get_UndoableActionCount(This,pcSeq)	\
	(This)->lpVtbl -> get_UndoableActionCount(This,pcSeq)

#define IActionHandler_get_UndoableSequenceCount(This,pcSeq)	\
	(This)->lpVtbl -> get_UndoableSequenceCount(This,pcSeq)

#define IActionHandler_get_RedoableSequenceCount(This,pcSeq)	\
	(This)->lpVtbl -> get_RedoableSequenceCount(This,pcSeq)

#define IActionHandler_get_UndoGrouper(This,ppundg)	\
	(This)->lpVtbl -> get_UndoGrouper(This,ppundg)

#define IActionHandler_put_UndoGrouper(This,pundg)	\
	(This)->lpVtbl -> put_UndoGrouper(This,pundg)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IActionHandler_BeginUndoTask_Proxy(
	IActionHandler * This,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo);


void __RPC_STUB IActionHandler_BeginUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_EndUndoTask_Proxy(
	IActionHandler * This);


void __RPC_STUB IActionHandler_EndUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_ContinueUndoTask_Proxy(
	IActionHandler * This);


void __RPC_STUB IActionHandler_ContinueUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_EndOuterUndoTask_Proxy(
	IActionHandler * This);


void __RPC_STUB IActionHandler_EndOuterUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_BreakUndoTask_Proxy(
	IActionHandler * This,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo);


void __RPC_STUB IActionHandler_BreakUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_StartSeq_Proxy(
	IActionHandler * This,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo,
	/* [in] */ IUndoAction *puact);


void __RPC_STUB IActionHandler_StartSeq_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_AddAction_Proxy(
	IActionHandler * This,
	/* [in] */ IUndoAction *puact);


void __RPC_STUB IActionHandler_AddAction_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_GetUndoText_Proxy(
	IActionHandler * This,
	/* [retval][out] */ BSTR *pbstrUndoText);


void __RPC_STUB IActionHandler_GetUndoText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_GetUndoTextN_Proxy(
	IActionHandler * This,
	/* [in] */ int iAct,
	/* [retval][out] */ BSTR *pbstrUndoText);


void __RPC_STUB IActionHandler_GetUndoTextN_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_GetRedoText_Proxy(
	IActionHandler * This,
	/* [retval][out] */ BSTR *pbstrRedoText);


void __RPC_STUB IActionHandler_GetRedoText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_GetRedoTextN_Proxy(
	IActionHandler * This,
	/* [in] */ int iAct,
	/* [retval][out] */ BSTR *pbstrRedoText);


void __RPC_STUB IActionHandler_GetRedoTextN_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_CanUndo_Proxy(
	IActionHandler * This,
	/* [retval][out] */ ComBool *pfCanUndo);


void __RPC_STUB IActionHandler_CanUndo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_CanRedo_Proxy(
	IActionHandler * This,
	/* [retval][out] */ ComBool *pfCanRedo);


void __RPC_STUB IActionHandler_CanRedo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_Undo_Proxy(
	IActionHandler * This,
	/* [retval][out] */ UndoResult *pures);


void __RPC_STUB IActionHandler_Undo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_Redo_Proxy(
	IActionHandler * This,
	/* [retval][out] */ UndoResult *pures);


void __RPC_STUB IActionHandler_Redo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_Commit_Proxy(
	IActionHandler * This);


void __RPC_STUB IActionHandler_Commit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_Close_Proxy(
	IActionHandler * This);


void __RPC_STUB IActionHandler_Close_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_Mark_Proxy(
	IActionHandler * This,
	/* [retval][out] */ int *phMark);


void __RPC_STUB IActionHandler_Mark_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_CollapseToMark_Proxy(
	IActionHandler * This,
	/* [in] */ int hMark,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo);


void __RPC_STUB IActionHandler_CollapseToMark_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActionHandler_DiscardToMark_Proxy(
	IActionHandler * This,
	/* [in] */ int hMark);


void __RPC_STUB IActionHandler_DiscardToMark_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_TopMarkHandle_Proxy(
	IActionHandler * This,
	/* [retval][out] */ int *phMark);


void __RPC_STUB IActionHandler_get_TopMarkHandle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_TasksSinceMark_Proxy(
	IActionHandler * This,
	/* [in] */ ComBool fUndo,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB IActionHandler_get_TasksSinceMark_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_UndoableActionCount_Proxy(
	IActionHandler * This,
	/* [retval][out] */ int *pcSeq);


void __RPC_STUB IActionHandler_get_UndoableActionCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_UndoableSequenceCount_Proxy(
	IActionHandler * This,
	/* [retval][out] */ int *pcSeq);


void __RPC_STUB IActionHandler_get_UndoableSequenceCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_RedoableSequenceCount_Proxy(
	IActionHandler * This,
	/* [retval][out] */ int *pcSeq);


void __RPC_STUB IActionHandler_get_RedoableSequenceCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IActionHandler_get_UndoGrouper_Proxy(
	IActionHandler * This,
	/* [retval][out] */ IUndoGrouper **ppundg);


void __RPC_STUB IActionHandler_get_UndoGrouper_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IActionHandler_put_UndoGrouper_Proxy(
	IActionHandler * This,
	/* [in] */ IUndoGrouper *pundg);


void __RPC_STUB IActionHandler_put_UndoGrouper_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IActionHandler_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_ActionHandler;

#ifdef __cplusplus

class DECLSPEC_UUID("CDED8B0B-5BD0-43be-96C4-6B8E8E7B017D")
ActionHandler;
#endif

#ifndef __IAdvInd_INTERFACE_DEFINED__
#define __IAdvInd_INTERFACE_DEFINED__

/* interface IAdvInd */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IAdvInd;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("5F74AB40-EFE8-4a0d-B9AE-30F493FE6E21")
	IAdvInd : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Step(
			/* [in] */ int nStepAmt) = 0;

	};

#else 	/* C style interface */

	typedef struct IAdvIndVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IAdvInd * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IAdvInd * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IAdvInd * This);

		HRESULT ( STDMETHODCALLTYPE *Step )(
			IAdvInd * This,
			/* [in] */ int nStepAmt);

		END_INTERFACE
	} IAdvIndVtbl;

	interface IAdvInd
	{
		CONST_VTBL struct IAdvIndVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IAdvInd_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAdvInd_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IAdvInd_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IAdvInd_Step(This,nStepAmt)	\
	(This)->lpVtbl -> Step(This,nStepAmt)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IAdvInd_Step_Proxy(
	IAdvInd * This,
	/* [in] */ int nStepAmt);


void __RPC_STUB IAdvInd_Step_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IAdvInd_INTERFACE_DEFINED__ */


#ifndef __IAdvInd2_INTERFACE_DEFINED__
#define __IAdvInd2_INTERFACE_DEFINED__

/* interface IAdvInd2 */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IAdvInd2;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("639C98DB-A241-496d-BE19-1EFC85CA1DD7")
	IAdvInd2 : public IAdvInd
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE NextStage( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IAdvInd2Vtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IAdvInd2 * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IAdvInd2 * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IAdvInd2 * This);

		HRESULT ( STDMETHODCALLTYPE *Step )(
			IAdvInd2 * This,
			/* [in] */ int nStepAmt);

		HRESULT ( STDMETHODCALLTYPE *NextStage )(
			IAdvInd2 * This);

		END_INTERFACE
	} IAdvInd2Vtbl;

	interface IAdvInd2
	{
		CONST_VTBL struct IAdvInd2Vtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IAdvInd2_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAdvInd2_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IAdvInd2_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IAdvInd2_Step(This,nStepAmt)	\
	(This)->lpVtbl -> Step(This,nStepAmt)


#define IAdvInd2_NextStage(This)	\
	(This)->lpVtbl -> NextStage(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IAdvInd2_NextStage_Proxy(
	IAdvInd2 * This);


void __RPC_STUB IAdvInd2_NextStage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IAdvInd2_INTERFACE_DEFINED__ */


#ifndef __IAdvInd3_INTERFACE_DEFINED__
#define __IAdvInd3_INTERFACE_DEFINED__

/* interface IAdvInd3 */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IAdvInd3;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("86b6ae62-3dfa-4020-b5d1-7fa28e7726e4")
	IAdvInd3 : public IAdvInd
	{
	public:
		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Title(
			/* [in] */ BSTR bstrTitle) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Message(
			/* [in] */ BSTR bstrMessage) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Position(
			/* [in] */ int nPos) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StepSize(
			/* [in] */ int nStepInc) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetRange(
			/* [in] */ int nMin,
			/* [in] */ int nMax) = 0;

	};

#else 	/* C style interface */

	typedef struct IAdvInd3Vtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IAdvInd3 * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IAdvInd3 * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IAdvInd3 * This);

		HRESULT ( STDMETHODCALLTYPE *Step )(
			IAdvInd3 * This,
			/* [in] */ int nStepAmt);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Title )(
			IAdvInd3 * This,
			/* [in] */ BSTR bstrTitle);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Message )(
			IAdvInd3 * This,
			/* [in] */ BSTR bstrMessage);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Position )(
			IAdvInd3 * This,
			/* [in] */ int nPos);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StepSize )(
			IAdvInd3 * This,
			/* [in] */ int nStepInc);

		HRESULT ( STDMETHODCALLTYPE *SetRange )(
			IAdvInd3 * This,
			/* [in] */ int nMin,
			/* [in] */ int nMax);

		END_INTERFACE
	} IAdvInd3Vtbl;

	interface IAdvInd3
	{
		CONST_VTBL struct IAdvInd3Vtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IAdvInd3_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAdvInd3_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IAdvInd3_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IAdvInd3_Step(This,nStepAmt)	\
	(This)->lpVtbl -> Step(This,nStepAmt)


#define IAdvInd3_put_Title(This,bstrTitle)	\
	(This)->lpVtbl -> put_Title(This,bstrTitle)

#define IAdvInd3_put_Message(This,bstrMessage)	\
	(This)->lpVtbl -> put_Message(This,bstrMessage)

#define IAdvInd3_put_Position(This,nPos)	\
	(This)->lpVtbl -> put_Position(This,nPos)

#define IAdvInd3_put_StepSize(This,nStepInc)	\
	(This)->lpVtbl -> put_StepSize(This,nStepInc)

#define IAdvInd3_SetRange(This,nMin,nMax)	\
	(This)->lpVtbl -> SetRange(This,nMin,nMax)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propput] */ HRESULT STDMETHODCALLTYPE IAdvInd3_put_Title_Proxy(
	IAdvInd3 * This,
	/* [in] */ BSTR bstrTitle);


void __RPC_STUB IAdvInd3_put_Title_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IAdvInd3_put_Message_Proxy(
	IAdvInd3 * This,
	/* [in] */ BSTR bstrMessage);


void __RPC_STUB IAdvInd3_put_Message_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IAdvInd3_put_Position_Proxy(
	IAdvInd3 * This,
	/* [in] */ int nPos);


void __RPC_STUB IAdvInd3_put_Position_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IAdvInd3_put_StepSize_Proxy(
	IAdvInd3 * This,
	/* [in] */ int nStepInc);


void __RPC_STUB IAdvInd3_put_StepSize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAdvInd3_SetRange_Proxy(
	IAdvInd3 * This,
	/* [in] */ int nMin,
	/* [in] */ int nMax);


void __RPC_STUB IAdvInd3_SetRange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IAdvInd3_INTERFACE_DEFINED__ */


#ifndef __IDebugReportSink_INTERFACE_DEFINED__
#define __IDebugReportSink_INTERFACE_DEFINED__

/* interface IDebugReportSink */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IDebugReportSink;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("14E389C6-C986-4e31-AE70-1CC10CC35471")
	IDebugReportSink : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Report(
			/* [in] */ CrtReportType nReportType,
			/* [in] */ BSTR szMsg) = 0;

	};

#else 	/* C style interface */

	typedef struct IDebugReportSinkVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDebugReportSink * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDebugReportSink * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDebugReportSink * This);

		HRESULT ( STDMETHODCALLTYPE *Report )(
			IDebugReportSink * This,
			/* [in] */ CrtReportType nReportType,
			/* [in] */ BSTR szMsg);

		END_INTERFACE
	} IDebugReportSinkVtbl;

	interface IDebugReportSink
	{
		CONST_VTBL struct IDebugReportSinkVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDebugReportSink_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDebugReportSink_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDebugReportSink_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDebugReportSink_Report(This,nReportType,szMsg)	\
	(This)->lpVtbl -> Report(This,nReportType,szMsg)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDebugReportSink_Report_Proxy(
	IDebugReportSink * This,
	/* [in] */ CrtReportType nReportType,
	/* [in] */ BSTR szMsg);


void __RPC_STUB IDebugReportSink_Report_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDebugReportSink_INTERFACE_DEFINED__ */


#ifndef __IDebugReport_INTERFACE_DEFINED__
#define __IDebugReport_INTERFACE_DEFINED__

/* interface IDebugReport */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IDebugReport;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("7AE7CF67-67BE-4860-8E72-AAC88294C397")
	IDebugReport : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ShowAssertMessageBox(
			/* [in] */ ComBool fShowMessageBox) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSink(
			/* [in] */ IDebugReportSink *pSink) = 0;

	};

#else 	/* C style interface */

	typedef struct IDebugReportVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDebugReport * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDebugReport * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDebugReport * This);

		HRESULT ( STDMETHODCALLTYPE *ShowAssertMessageBox )(
			IDebugReport * This,
			/* [in] */ ComBool fShowMessageBox);

		HRESULT ( STDMETHODCALLTYPE *SetSink )(
			IDebugReport * This,
			/* [in] */ IDebugReportSink *pSink);

		END_INTERFACE
	} IDebugReportVtbl;

	interface IDebugReport
	{
		CONST_VTBL struct IDebugReportVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDebugReport_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDebugReport_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDebugReport_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDebugReport_ShowAssertMessageBox(This,fShowMessageBox)	\
	(This)->lpVtbl -> ShowAssertMessageBox(This,fShowMessageBox)

#define IDebugReport_SetSink(This,pSink)	\
	(This)->lpVtbl -> SetSink(This,pSink)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDebugReport_ShowAssertMessageBox_Proxy(
	IDebugReport * This,
	/* [in] */ ComBool fShowMessageBox);


void __RPC_STUB IDebugReport_ShowAssertMessageBox_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDebugReport_SetSink_Proxy(
	IDebugReport * This,
	/* [in] */ IDebugReportSink *pSink);


void __RPC_STUB IDebugReport_SetSink_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDebugReport_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_DebugReport;

#ifdef __cplusplus

class DECLSPEC_UUID("24636FD1-DB8D-4b2c-B4C0-44C2592CA482")
DebugReport;
#endif

#ifndef __IHelpTopicProvider_INTERFACE_DEFINED__
#define __IHelpTopicProvider_INTERFACE_DEFINED__

/* interface IHelpTopicProvider */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IHelpTopicProvider;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("AF8960FB-B7AF-4259-832B-38A3F5629052")
	IHelpTopicProvider : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE GetHelpString(
			/* [in] */ BSTR bstrPropName,
			/* [in] */ int iKey,
			/* [retval][out] */ BSTR *pbstrPropValue) = 0;

	};

#else 	/* C style interface */

	typedef struct IHelpTopicProviderVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IHelpTopicProvider * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IHelpTopicProvider * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IHelpTopicProvider * This);

		HRESULT ( STDMETHODCALLTYPE *GetHelpString )(
			IHelpTopicProvider * This,
			/* [in] */ BSTR bstrPropName,
			/* [in] */ int iKey,
			/* [retval][out] */ BSTR *pbstrPropValue);

		END_INTERFACE
	} IHelpTopicProviderVtbl;

	interface IHelpTopicProvider
	{
		CONST_VTBL struct IHelpTopicProviderVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IHelpTopicProvider_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IHelpTopicProvider_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IHelpTopicProvider_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IHelpTopicProvider_GetHelpString(This,bstrPropName,iKey,pbstrPropValue)	\
	(This)->lpVtbl -> GetHelpString(This,bstrPropName,iKey,pbstrPropValue)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IHelpTopicProvider_GetHelpString_Proxy(
	IHelpTopicProvider * This,
	/* [in] */ BSTR bstrPropName,
	/* [in] */ int iKey,
	/* [retval][out] */ BSTR *pbstrPropValue);


void __RPC_STUB IHelpTopicProvider_GetHelpString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IHelpTopicProvider_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwFldSpec;

#ifdef __cplusplus

class DECLSPEC_UUID("51C4C464-12D2-4CB8-96F3-66E18A6A3AC6")
FwFldSpec;
#endif

#ifndef __ITsTextProps_INTERFACE_DEFINED__
#define __ITsTextProps_INTERFACE_DEFINED__

/* interface ITsTextProps */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsTextProps;

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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsTextProps_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsTextProps_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsTextProps_get_IntPropCount(This,pcv)	\
	(This)->lpVtbl -> get_IntPropCount(This,pcv)

#define ITsTextProps_GetIntProp(This,iv,ptpt,pnVar,pnVal)	\
	(This)->lpVtbl -> GetIntProp(This,iv,ptpt,pnVar,pnVal)

#define ITsTextProps_GetIntPropValues(This,tpt,pnVar,pnVal)	\
	(This)->lpVtbl -> GetIntPropValues(This,tpt,pnVar,pnVal)

#define ITsTextProps_get_StrPropCount(This,pcv)	\
	(This)->lpVtbl -> get_StrPropCount(This,pcv)

#define ITsTextProps_GetStrProp(This,iv,ptpt,pbstrVal)	\
	(This)->lpVtbl -> GetStrProp(This,iv,ptpt,pbstrVal)

#define ITsTextProps_GetStrPropValue(This,tpt,pbstrVal)	\
	(This)->lpVtbl -> GetStrPropValue(This,tpt,pbstrVal)

#define ITsTextProps_GetBldr(This,pptpb)	\
	(This)->lpVtbl -> GetBldr(This,pptpb)

#define ITsTextProps_GetFactoryClsid(This,pclsid)	\
	(This)->lpVtbl -> GetFactoryClsid(This,pclsid)

#define ITsTextProps_Serialize(This,pstrm)	\
	(This)->lpVtbl -> Serialize(This,pstrm)

#define ITsTextProps_SerializeRgb(This,prgb,cbMax,pcb)	\
	(This)->lpVtbl -> SerializeRgb(This,prgb,cbMax,pcb)

#define ITsTextProps_SerializeRgPropsRgb(This,cpttp,rgpttp,rgich,prgb,cbMax,pcb)	\
	(This)->lpVtbl -> SerializeRgPropsRgb(This,cpttp,rgpttp,rgich,prgb,cbMax,pcb)

#define ITsTextProps_WriteAsXml(This,pstrm,pwsf,cchIndent)	\
	(This)->lpVtbl -> WriteAsXml(This,pstrm,pwsf,cchIndent)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ITsTextProps_get_IntPropCount_Proxy(
	ITsTextProps * This,
	/* [retval][out] */ int *pcv);


void __RPC_STUB ITsTextProps_get_IntPropCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetIntProp_Proxy(
	ITsTextProps * This,
	/* [in] */ int iv,
	/* [out] */ int *ptpt,
	/* [out] */ int *pnVar,
	/* [retval][out] */ int *pnVal);


void __RPC_STUB ITsTextProps_GetIntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetIntPropValues_Proxy(
	ITsTextProps * This,
	/* [in] */ int tpt,
	/* [out] */ int *pnVar,
	/* [retval][out] */ int *pnVal);


void __RPC_STUB ITsTextProps_GetIntPropValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsTextProps_get_StrPropCount_Proxy(
	ITsTextProps * This,
	/* [retval][out] */ int *pcv);


void __RPC_STUB ITsTextProps_get_StrPropCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetStrProp_Proxy(
	ITsTextProps * This,
	/* [in] */ int iv,
	/* [out] */ int *ptpt,
	/* [retval][out] */ BSTR *pbstrVal);


void __RPC_STUB ITsTextProps_GetStrProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetStrPropValue_Proxy(
	ITsTextProps * This,
	/* [in] */ int tpt,
	/* [retval][out] */ BSTR *pbstrVal);


void __RPC_STUB ITsTextProps_GetStrPropValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetBldr_Proxy(
	ITsTextProps * This,
	/* [retval][out] */ ITsPropsBldr **pptpb);


void __RPC_STUB ITsTextProps_GetBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_GetFactoryClsid_Proxy(
	ITsTextProps * This,
	/* [retval][out] */ CLSID *pclsid);


void __RPC_STUB ITsTextProps_GetFactoryClsid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_Serialize_Proxy(
	ITsTextProps * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB ITsTextProps_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_SerializeRgb_Proxy(
	ITsTextProps * This,
	/* [size_is][out] */ BYTE *prgb,
	/* [in] */ int cbMax,
	/* [retval][out] */ int *pcb);


void __RPC_STUB ITsTextProps_SerializeRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_SerializeRgPropsRgb_Proxy(
	ITsTextProps * This,
	/* [in] */ int cpttp,
	/* [in] */ ITsTextProps **rgpttp,
	/* [in] */ int *rgich,
	/* [size_is][out] */ BYTE *prgb,
	/* [in] */ int cbMax,
	/* [retval][out] */ int *pcb);


void __RPC_STUB ITsTextProps_SerializeRgPropsRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsTextProps_WriteAsXml_Proxy(
	ITsTextProps * This,
	/* [in] */ IStream *pstrm,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [in] */ int cchIndent);


void __RPC_STUB ITsTextProps_WriteAsXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsTextProps_INTERFACE_DEFINED__ */


#ifndef __ITsStrFactory_INTERFACE_DEFINED__
#define __ITsStrFactory_INTERFACE_DEFINED__

/* interface ITsStrFactory */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsStrFactory;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F1EF76E4-BE04-11d3-8D9A-005004DEFEC4")
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
			/* [string][size_is][in] */ const OLECHAR *prgchTxt,
			/* [out][in] */ int *pcchTxt,
			/* [size_is][in] */ const BYTE *prgbFmt,
			/* [out][in] */ int *pcbFmt,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeString(
			/* [in] */ BSTR bstr,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE MakeStringRgch(
			/* [string][size_is][in] */ const OLECHAR *prgch,
			/* [in] */ int cch,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE MakeStringWithPropsRgch(
			/* [string][size_is][in] */ const OLECHAR *prgch,
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

	};

#else 	/* C style interface */

	typedef struct ITsStrFactoryVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsStrFactory * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [string][size_is][in] */ const OLECHAR *prgchTxt,
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
			/* [string][size_is][in] */ const OLECHAR *prgch,
			/* [in] */ int cch,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *MakeStringWithPropsRgch )(
			ITsStrFactory * This,
			/* [string][size_is][in] */ const OLECHAR *prgch,
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

		END_INTERFACE
	} ITsStrFactoryVtbl;

	interface ITsStrFactory
	{
		CONST_VTBL struct ITsStrFactoryVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ITsStrFactory_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsStrFactory_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsStrFactory_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsStrFactory_DeserializeStringStreams(This,pstrmTxt,pstrmFmt,pptss)	\
	(This)->lpVtbl -> DeserializeStringStreams(This,pstrmTxt,pstrmFmt,pptss)

#define ITsStrFactory_DeserializeString(This,bstrTxt,pstrmFmt,pptss)	\
	(This)->lpVtbl -> DeserializeString(This,bstrTxt,pstrmFmt,pptss)

#define ITsStrFactory_DeserializeStringRgb(This,bstrTxt,prgbFmt,cbFmt,pptss)	\
	(This)->lpVtbl -> DeserializeStringRgb(This,bstrTxt,prgbFmt,cbFmt,pptss)

#define ITsStrFactory_DeserializeStringRgch(This,prgchTxt,pcchTxt,prgbFmt,pcbFmt,pptss)	\
	(This)->lpVtbl -> DeserializeStringRgch(This,prgchTxt,pcchTxt,prgbFmt,pcbFmt,pptss)

#define ITsStrFactory_MakeString(This,bstr,ws,pptss)	\
	(This)->lpVtbl -> MakeString(This,bstr,ws,pptss)

#define ITsStrFactory_MakeStringRgch(This,prgch,cch,ws,pptss)	\
	(This)->lpVtbl -> MakeStringRgch(This,prgch,cch,ws,pptss)

#define ITsStrFactory_MakeStringWithPropsRgch(This,prgch,cch,pttp,pptss)	\
	(This)->lpVtbl -> MakeStringWithPropsRgch(This,prgch,cch,pttp,pptss)

#define ITsStrFactory_GetBldr(This,pptsb)	\
	(This)->lpVtbl -> GetBldr(This,pptsb)

#define ITsStrFactory_GetIncBldr(This,pptisb)	\
	(This)->lpVtbl -> GetIncBldr(This,pptisb)

#define ITsStrFactory_get_RunCount(This,prgbFmt,cbFmt,pcrun)	\
	(This)->lpVtbl -> get_RunCount(This,prgbFmt,cbFmt,pcrun)

#define ITsStrFactory_FetchRunInfoAt(This,prgbFmt,cbFmt,ich,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfoAt(This,prgbFmt,cbFmt,ich,ptri,ppttp)

#define ITsStrFactory_FetchRunInfo(This,prgbFmt,cbFmt,irun,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfo(This,prgbFmt,cbFmt,irun,ptri,ppttp)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ITsStrFactory_DeserializeStringStreams_Proxy(
	ITsStrFactory * This,
	/* [in] */ IStream *pstrmTxt,
	/* [in] */ IStream *pstrmFmt,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_DeserializeStringStreams_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_DeserializeString_Proxy(
	ITsStrFactory * This,
	/* [in] */ BSTR bstrTxt,
	/* [in] */ IStream *pstrmFmt,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_DeserializeString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_DeserializeStringRgb_Proxy(
	ITsStrFactory * This,
	/* [in] */ BSTR bstrTxt,
	/* [size_is][in] */ const BYTE *prgbFmt,
	/* [in] */ int cbFmt,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_DeserializeStringRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_DeserializeStringRgch_Proxy(
	ITsStrFactory * This,
	/* [string][size_is][in] */ const OLECHAR *prgchTxt,
	/* [out][in] */ int *pcchTxt,
	/* [size_is][in] */ const BYTE *prgbFmt,
	/* [out][in] */ int *pcbFmt,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_DeserializeStringRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_MakeString_Proxy(
	ITsStrFactory * This,
	/* [in] */ BSTR bstr,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_MakeString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsStrFactory_MakeStringRgch_Proxy(
	ITsStrFactory * This,
	/* [string][size_is][in] */ const OLECHAR *prgch,
	/* [in] */ int cch,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_MakeStringRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsStrFactory_MakeStringWithPropsRgch_Proxy(
	ITsStrFactory * This,
	/* [string][size_is][in] */ const OLECHAR *prgch,
	/* [in] */ int cch,
	/* [in] */ ITsTextProps *pttp,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrFactory_MakeStringWithPropsRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_GetBldr_Proxy(
	ITsStrFactory * This,
	/* [retval][out] */ ITsStrBldr **pptsb);


void __RPC_STUB ITsStrFactory_GetBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_GetIncBldr_Proxy(
	ITsStrFactory * This,
	/* [retval][out] */ ITsIncStrBldr **pptisb);


void __RPC_STUB ITsStrFactory_GetIncBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrFactory_get_RunCount_Proxy(
	ITsStrFactory * This,
	/* [size_is][in] */ const BYTE *prgbFmt,
	/* [in] */ int cbFmt,
	/* [retval][out] */ int *pcrun);


void __RPC_STUB ITsStrFactory_get_RunCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_FetchRunInfoAt_Proxy(
	ITsStrFactory * This,
	/* [size_is][in] */ const BYTE *prgbFmt,
	/* [in] */ int cbFmt,
	/* [in] */ int ich,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsStrFactory_FetchRunInfoAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrFactory_FetchRunInfo_Proxy(
	ITsStrFactory * This,
	/* [size_is][in] */ const BYTE *prgbFmt,
	/* [in] */ int cbFmt,
	/* [in] */ int irun,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsStrFactory_FetchRunInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsStrFactory_INTERFACE_DEFINED__ */


#ifndef __ITsPropsFactory_INTERFACE_DEFINED__
#define __ITsPropsFactory_INTERFACE_DEFINED__

/* interface ITsPropsFactory */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsPropsFactory;

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
			/* [string][size_is][in] */ const OLECHAR *prgchStyle,
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
			/* [iid_is][out] */ void **ppvObject);

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
			/* [string][size_is][in] */ const OLECHAR *prgchStyle,
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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsPropsFactory_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsPropsFactory_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsPropsFactory_DeserializeProps(This,pstrm,ppttp)	\
	(This)->lpVtbl -> DeserializeProps(This,pstrm,ppttp)

#define ITsPropsFactory_DeserializePropsRgb(This,prgb,pcb,ppttp)	\
	(This)->lpVtbl -> DeserializePropsRgb(This,prgb,pcb,ppttp)

#define ITsPropsFactory_DeserializeRgPropsRgb(This,cpttpMax,prgb,pcb,pcpttpRet,rgpttp,rgich)	\
	(This)->lpVtbl -> DeserializeRgPropsRgb(This,cpttpMax,prgb,pcb,pcpttpRet,rgpttp,rgich)

#define ITsPropsFactory_MakeProps(This,bstrStyle,ws,ows,ppttp)	\
	(This)->lpVtbl -> MakeProps(This,bstrStyle,ws,ows,ppttp)

#define ITsPropsFactory_MakePropsRgch(This,prgchStyle,cch,ws,ows,ppttp)	\
	(This)->lpVtbl -> MakePropsRgch(This,prgchStyle,cch,ws,ows,ppttp)

#define ITsPropsFactory_GetPropsBldr(This,pptpb)	\
	(This)->lpVtbl -> GetPropsBldr(This,pptpb)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ITsPropsFactory_DeserializeProps_Proxy(
	ITsPropsFactory * This,
	/* [in] */ IStream *pstrm,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsPropsFactory_DeserializeProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsFactory_DeserializePropsRgb_Proxy(
	ITsPropsFactory * This,
	/* [size_is][in] */ const BYTE *prgb,
	/* [out][in] */ int *pcb,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsPropsFactory_DeserializePropsRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsFactory_DeserializeRgPropsRgb_Proxy(
	ITsPropsFactory * This,
	/* [in] */ int cpttpMax,
	/* [size_is][in] */ const BYTE *prgb,
	/* [out][in] */ int *pcb,
	/* [out] */ int *pcpttpRet,
	/* [size_is][out] */ ITsTextProps **rgpttp,
	/* [size_is][out] */ int *rgich);


void __RPC_STUB ITsPropsFactory_DeserializeRgPropsRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsFactory_MakeProps_Proxy(
	ITsPropsFactory * This,
	/* [in] */ BSTR bstrStyle,
	/* [in] */ int ws,
	/* [in] */ int ows,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsPropsFactory_MakeProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsPropsFactory_MakePropsRgch_Proxy(
	ITsPropsFactory * This,
	/* [string][size_is][in] */ const OLECHAR *prgchStyle,
	/* [in] */ int cch,
	/* [in] */ int ws,
	/* [in] */ int ows,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsPropsFactory_MakePropsRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsFactory_GetPropsBldr_Proxy(
	ITsPropsFactory * This,
	/* [retval][out] */ ITsPropsBldr **pptpb);


void __RPC_STUB ITsPropsFactory_GetPropsBldr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsPropsFactory_INTERFACE_DEFINED__ */


#ifndef __ITsStrBldr_INTERFACE_DEFINED__
#define __ITsStrBldr_INTERFACE_DEFINED__

/* interface ITsStrBldr */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsStrBldr;

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
			/* [string][size_is][out] */ OLECHAR *prgch) = 0;

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
			/* [string][size_is][in] */ const OLECHAR *prgchIns,
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
			/* [iid_is][out] */ void **ppvObject);

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
			/* [string][size_is][out] */ OLECHAR *prgch);

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
			/* [string][size_is][in] */ const OLECHAR *prgchIns,
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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsStrBldr_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsStrBldr_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsStrBldr_get_Text(This,pbstr)	\
	(This)->lpVtbl -> get_Text(This,pbstr)

#define ITsStrBldr_get_Length(This,pcch)	\
	(This)->lpVtbl -> get_Length(This,pcch)

#define ITsStrBldr_get_RunCount(This,pcrun)	\
	(This)->lpVtbl -> get_RunCount(This,pcrun)

#define ITsStrBldr_get_RunAt(This,ich,pirun)	\
	(This)->lpVtbl -> get_RunAt(This,ich,pirun)

#define ITsStrBldr_GetBoundsOfRun(This,irun,pichMin,pichLim)	\
	(This)->lpVtbl -> GetBoundsOfRun(This,irun,pichMin,pichLim)

#define ITsStrBldr_FetchRunInfoAt(This,ich,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfoAt(This,ich,ptri,ppttp)

#define ITsStrBldr_FetchRunInfo(This,irun,ptri,ppttp)	\
	(This)->lpVtbl -> FetchRunInfo(This,irun,ptri,ppttp)

#define ITsStrBldr_get_RunText(This,irun,pbstr)	\
	(This)->lpVtbl -> get_RunText(This,irun,pbstr)

#define ITsStrBldr_GetChars(This,ichMin,ichLim,pbstr)	\
	(This)->lpVtbl -> GetChars(This,ichMin,ichLim,pbstr)

#define ITsStrBldr_FetchChars(This,ichMin,ichLim,prgch)	\
	(This)->lpVtbl -> FetchChars(This,ichMin,ichLim,prgch)

#define ITsStrBldr_get_PropertiesAt(This,ich,pttp)	\
	(This)->lpVtbl -> get_PropertiesAt(This,ich,pttp)

#define ITsStrBldr_get_Properties(This,irun,pttp)	\
	(This)->lpVtbl -> get_Properties(This,irun,pttp)

#define ITsStrBldr_Replace(This,ichMin,ichLim,bstrIns,pttp)	\
	(This)->lpVtbl -> Replace(This,ichMin,ichLim,bstrIns,pttp)

#define ITsStrBldr_ReplaceTsString(This,ichMin,ichLim,ptssIns)	\
	(This)->lpVtbl -> ReplaceTsString(This,ichMin,ichLim,ptssIns)

#define ITsStrBldr_ReplaceRgch(This,ichMin,ichLim,prgchIns,cchIns,pttp)	\
	(This)->lpVtbl -> ReplaceRgch(This,ichMin,ichLim,prgchIns,cchIns,pttp)

#define ITsStrBldr_SetProperties(This,ichMin,ichLim,pttp)	\
	(This)->lpVtbl -> SetProperties(This,ichMin,ichLim,pttp)

#define ITsStrBldr_SetIntPropValues(This,ichMin,ichLim,tpt,nVar,nVal)	\
	(This)->lpVtbl -> SetIntPropValues(This,ichMin,ichLim,tpt,nVar,nVal)

#define ITsStrBldr_SetStrPropValue(This,ichMin,ichLim,tpt,bstrVal)	\
	(This)->lpVtbl -> SetStrPropValue(This,ichMin,ichLim,tpt,bstrVal)

#define ITsStrBldr_GetString(This,pptss)	\
	(This)->lpVtbl -> GetString(This,pptss)

#define ITsStrBldr_Clear(This)	\
	(This)->lpVtbl -> Clear(This)

#define ITsStrBldr_GetBldrClsid(This,pclsid)	\
	(This)->lpVtbl -> GetBldrClsid(This,pclsid)

#define ITsStrBldr_SerializeFmt(This,pstrm)	\
	(This)->lpVtbl -> SerializeFmt(This,pstrm)

#define ITsStrBldr_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	(This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [id][propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_Text_Proxy(
	ITsStrBldr * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsStrBldr_get_Text_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_Length_Proxy(
	ITsStrBldr * This,
	/* [retval][out] */ int *pcch);


void __RPC_STUB ITsStrBldr_get_Length_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_RunCount_Proxy(
	ITsStrBldr * This,
	/* [retval][out] */ int *pcrun);


void __RPC_STUB ITsStrBldr_get_RunCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_RunAt_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ich,
	/* [retval][out] */ int *pirun);


void __RPC_STUB ITsStrBldr_get_RunAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_GetBoundsOfRun_Proxy(
	ITsStrBldr * This,
	/* [in] */ int irun,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB ITsStrBldr_GetBoundsOfRun_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_FetchRunInfoAt_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ich,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsStrBldr_FetchRunInfoAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_FetchRunInfo_Proxy(
	ITsStrBldr * This,
	/* [in] */ int irun,
	/* [out] */ TsRunInfo *ptri,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsStrBldr_FetchRunInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_RunText_Proxy(
	ITsStrBldr * This,
	/* [in] */ int irun,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsStrBldr_get_RunText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_GetChars_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsStrBldr_GetChars_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local][restricted] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_FetchChars_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [string][size_is][out] */ OLECHAR *prgch);


void __RPC_STUB ITsStrBldr_FetchChars_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_PropertiesAt_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ich,
	/* [retval][out] */ ITsTextProps **pttp);


void __RPC_STUB ITsStrBldr_get_PropertiesAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStrBldr_get_Properties_Proxy(
	ITsStrBldr * This,
	/* [in] */ int irun,
	/* [retval][out] */ ITsTextProps **pttp);


void __RPC_STUB ITsStrBldr_get_Properties_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_Replace_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ BSTR bstrIns,
	/* [in] */ ITsTextProps *pttp);


void __RPC_STUB ITsStrBldr_Replace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_ReplaceTsString_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ ITsString *ptssIns);


void __RPC_STUB ITsStrBldr_ReplaceTsString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_ReplaceRgch_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [string][size_is][in] */ const OLECHAR *prgchIns,
	/* [in] */ int cchIns,
	/* [in] */ ITsTextProps *pttp);


void __RPC_STUB ITsStrBldr_ReplaceRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_SetProperties_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ ITsTextProps *pttp);


void __RPC_STUB ITsStrBldr_SetProperties_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_SetIntPropValues_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ int tpt,
	/* [in] */ int nVar,
	/* [in] */ int nVal);


void __RPC_STUB ITsStrBldr_SetIntPropValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_SetStrPropValue_Proxy(
	ITsStrBldr * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ int tpt,
	/* [in] */ BSTR bstrVal);


void __RPC_STUB ITsStrBldr_SetStrPropValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_GetString_Proxy(
	ITsStrBldr * This,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStrBldr_GetString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_Clear_Proxy(
	ITsStrBldr * This);


void __RPC_STUB ITsStrBldr_Clear_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_GetBldrClsid_Proxy(
	ITsStrBldr * This,
	/* [retval][out] */ CLSID *pclsid);


void __RPC_STUB ITsStrBldr_GetBldrClsid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_SerializeFmt_Proxy(
	ITsStrBldr * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB ITsStrBldr_SerializeFmt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStrBldr_SerializeFmtRgb_Proxy(
	ITsStrBldr * This,
	/* [size_is][out] */ BYTE *prgb,
	/* [in] */ int cbMax,
	/* [retval][out] */ int *pcbNeeded);


void __RPC_STUB ITsStrBldr_SerializeFmtRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsStrBldr_INTERFACE_DEFINED__ */


#ifndef __ITsIncStrBldr_INTERFACE_DEFINED__
#define __ITsIncStrBldr_INTERFACE_DEFINED__

/* interface ITsIncStrBldr */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsIncStrBldr;

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
			/* [string][size_is][in] */ const OLECHAR *prgchIns,
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

	};

#else 	/* C style interface */

	typedef struct ITsIncStrBldrVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsIncStrBldr * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [string][size_is][in] */ const OLECHAR *prgchIns,
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

		END_INTERFACE
	} ITsIncStrBldrVtbl;

	interface ITsIncStrBldr
	{
		CONST_VTBL struct ITsIncStrBldrVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ITsIncStrBldr_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsIncStrBldr_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsIncStrBldr_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsIncStrBldr_get_Text(This,pbstr)	\
	(This)->lpVtbl -> get_Text(This,pbstr)

#define ITsIncStrBldr_Append(This,bstrIns)	\
	(This)->lpVtbl -> Append(This,bstrIns)

#define ITsIncStrBldr_AppendTsString(This,ptssIns)	\
	(This)->lpVtbl -> AppendTsString(This,ptssIns)

#define ITsIncStrBldr_AppendRgch(This,prgchIns,cchIns)	\
	(This)->lpVtbl -> AppendRgch(This,prgchIns,cchIns)

#define ITsIncStrBldr_SetIntPropValues(This,tpt,nVar,nVal)	\
	(This)->lpVtbl -> SetIntPropValues(This,tpt,nVar,nVal)

#define ITsIncStrBldr_SetStrPropValue(This,tpt,bstrVal)	\
	(This)->lpVtbl -> SetStrPropValue(This,tpt,bstrVal)

#define ITsIncStrBldr_GetString(This,pptss)	\
	(This)->lpVtbl -> GetString(This,pptss)

#define ITsIncStrBldr_Clear(This)	\
	(This)->lpVtbl -> Clear(This)

#define ITsIncStrBldr_GetIncBldrClsid(This,pclsid)	\
	(This)->lpVtbl -> GetIncBldrClsid(This,pclsid)

#define ITsIncStrBldr_SerializeFmt(This,pstrm)	\
	(This)->lpVtbl -> SerializeFmt(This,pstrm)

#define ITsIncStrBldr_SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)	\
	(This)->lpVtbl -> SerializeFmtRgb(This,prgb,cbMax,pcbNeeded)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [id][propget] */ HRESULT STDMETHODCALLTYPE ITsIncStrBldr_get_Text_Proxy(
	ITsIncStrBldr * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsIncStrBldr_get_Text_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_Append_Proxy(
	ITsIncStrBldr * This,
	/* [in] */ BSTR bstrIns);


void __RPC_STUB ITsIncStrBldr_Append_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_AppendTsString_Proxy(
	ITsIncStrBldr * This,
	/* [in] */ ITsString *ptssIns);


void __RPC_STUB ITsIncStrBldr_AppendTsString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ITsIncStrBldr_AppendRgch_Proxy(
	ITsIncStrBldr * This,
	/* [string][size_is][in] */ const OLECHAR *prgchIns,
	/* [in] */ int cchIns);


void __RPC_STUB ITsIncStrBldr_AppendRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_SetIntPropValues_Proxy(
	ITsIncStrBldr * This,
	/* [in] */ int tpt,
	/* [in] */ int nVar,
	/* [in] */ int nVal);


void __RPC_STUB ITsIncStrBldr_SetIntPropValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_SetStrPropValue_Proxy(
	ITsIncStrBldr * This,
	/* [in] */ int tpt,
	/* [in] */ BSTR bstrVal);


void __RPC_STUB ITsIncStrBldr_SetStrPropValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_GetString_Proxy(
	ITsIncStrBldr * This,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsIncStrBldr_GetString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_Clear_Proxy(
	ITsIncStrBldr * This);


void __RPC_STUB ITsIncStrBldr_Clear_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_GetIncBldrClsid_Proxy(
	ITsIncStrBldr * This,
	/* [retval][out] */ CLSID *pclsid);


void __RPC_STUB ITsIncStrBldr_GetIncBldrClsid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_SerializeFmt_Proxy(
	ITsIncStrBldr * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB ITsIncStrBldr_SerializeFmt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsIncStrBldr_SerializeFmtRgb_Proxy(
	ITsIncStrBldr * This,
	/* [size_is][out] */ BYTE *prgb,
	/* [in] */ int cbMax,
	/* [retval][out] */ int *pcbNeeded);


void __RPC_STUB ITsIncStrBldr_SerializeFmtRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsIncStrBldr_INTERFACE_DEFINED__ */


#ifndef __ITsPropsBldr_INTERFACE_DEFINED__
#define __ITsPropsBldr_INTERFACE_DEFINED__

/* interface ITsPropsBldr */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsPropsBldr;

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

	};

#else 	/* C style interface */

	typedef struct ITsPropsBldrVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsPropsBldr * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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

		END_INTERFACE
	} ITsPropsBldrVtbl;

	interface ITsPropsBldr
	{
		CONST_VTBL struct ITsPropsBldrVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ITsPropsBldr_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsPropsBldr_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsPropsBldr_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsPropsBldr_get_IntPropCount(This,pcv)	\
	(This)->lpVtbl -> get_IntPropCount(This,pcv)

#define ITsPropsBldr_GetIntProp(This,iv,ptpt,pnVar,pnVal)	\
	(This)->lpVtbl -> GetIntProp(This,iv,ptpt,pnVar,pnVal)

#define ITsPropsBldr_GetIntPropValues(This,tpt,pnVar,pnVal)	\
	(This)->lpVtbl -> GetIntPropValues(This,tpt,pnVar,pnVal)

#define ITsPropsBldr_get_StrPropCount(This,pcv)	\
	(This)->lpVtbl -> get_StrPropCount(This,pcv)

#define ITsPropsBldr_GetStrProp(This,iv,ptpt,pbstrVal)	\
	(This)->lpVtbl -> GetStrProp(This,iv,ptpt,pbstrVal)

#define ITsPropsBldr_GetStrPropValue(This,tpt,pbstrVal)	\
	(This)->lpVtbl -> GetStrPropValue(This,tpt,pbstrVal)

#define ITsPropsBldr_SetIntPropValues(This,tpt,nVar,nVal)	\
	(This)->lpVtbl -> SetIntPropValues(This,tpt,nVar,nVal)

#define ITsPropsBldr_SetStrPropValue(This,tpt,bstrVal)	\
	(This)->lpVtbl -> SetStrPropValue(This,tpt,bstrVal)

#define ITsPropsBldr_SetStrPropValueRgch(This,tpt,rgchVal,nValLength)	\
	(This)->lpVtbl -> SetStrPropValueRgch(This,tpt,rgchVal,nValLength)

#define ITsPropsBldr_GetTextProps(This,ppttp)	\
	(This)->lpVtbl -> GetTextProps(This,ppttp)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ITsPropsBldr_get_IntPropCount_Proxy(
	ITsPropsBldr * This,
	/* [retval][out] */ int *pcv);


void __RPC_STUB ITsPropsBldr_get_IntPropCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_GetIntProp_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int iv,
	/* [out] */ int *ptpt,
	/* [out] */ int *pnVar,
	/* [out] */ int *pnVal);


void __RPC_STUB ITsPropsBldr_GetIntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_GetIntPropValues_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int tpt,
	/* [out] */ int *pnVar,
	/* [out] */ int *pnVal);


void __RPC_STUB ITsPropsBldr_GetIntPropValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsPropsBldr_get_StrPropCount_Proxy(
	ITsPropsBldr * This,
	/* [retval][out] */ int *pcv);


void __RPC_STUB ITsPropsBldr_get_StrPropCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_GetStrProp_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int iv,
	/* [out] */ int *ptpt,
	/* [out] */ BSTR *pbstrVal);


void __RPC_STUB ITsPropsBldr_GetStrProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_GetStrPropValue_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int tpt,
	/* [out] */ BSTR *pbstrVal);


void __RPC_STUB ITsPropsBldr_GetStrPropValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_SetIntPropValues_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int tpt,
	/* [in] */ int nVar,
	/* [in] */ int nVal);


void __RPC_STUB ITsPropsBldr_SetIntPropValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_SetStrPropValue_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int tpt,
	/* [in] */ BSTR bstrVal);


void __RPC_STUB ITsPropsBldr_SetStrPropValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_SetStrPropValueRgch_Proxy(
	ITsPropsBldr * This,
	/* [in] */ int tpt,
	/* [size_is][in] */ const byte *rgchVal,
	/* [in] */ int nValLength);


void __RPC_STUB ITsPropsBldr_SetStrPropValueRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsPropsBldr_GetTextProps_Proxy(
	ITsPropsBldr * This,
	/* [retval][out] */ ITsTextProps **ppttp);


void __RPC_STUB ITsPropsBldr_GetTextProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsPropsBldr_INTERFACE_DEFINED__ */


#ifndef __ITsMultiString_INTERFACE_DEFINED__
#define __ITsMultiString_INTERFACE_DEFINED__

/* interface ITsMultiString */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsMultiString;

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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsMultiString_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsMultiString_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsMultiString_get_StringCount(This,pctss)	\
	(This)->lpVtbl -> get_StringCount(This,pctss)

#define ITsMultiString_GetStringFromIndex(This,iws,pws,pptss)	\
	(This)->lpVtbl -> GetStringFromIndex(This,iws,pws,pptss)

#define ITsMultiString_get_String(This,ws,pptss)	\
	(This)->lpVtbl -> get_String(This,ws,pptss)

#define ITsMultiString_putref_String(This,ws,ptss)	\
	(This)->lpVtbl -> putref_String(This,ws,ptss)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ITsMultiString_get_StringCount_Proxy(
	ITsMultiString * This,
	/* [retval][out] */ int *pctss);


void __RPC_STUB ITsMultiString_get_StringCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsMultiString_GetStringFromIndex_Proxy(
	ITsMultiString * This,
	/* [in] */ int iws,
	/* [out] */ int *pws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsMultiString_GetStringFromIndex_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsMultiString_get_String_Proxy(
	ITsMultiString * This,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsMultiString_get_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ITsMultiString_putref_String_Proxy(
	ITsMultiString * This,
	/* [in] */ int ws,
	/* [in] */ ITsString *ptss);


void __RPC_STUB ITsMultiString_putref_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsMultiString_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystemFactory_INTERFACE_DEFINED__
#define __ILgWritingSystemFactory_INTERFACE_DEFINED__

/* interface ILgWritingSystemFactory */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgWritingSystemFactory;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2C4636E3-4F49-4966-966F-0953F97F51C8")
	ILgWritingSystemFactory : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Engine(
			/* [in] */ BSTR bstrIcuLocale,
			/* [retval][out] */ ILgWritingSystem **ppwseng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EngineOrNull(
			/* [in] */ int ws,
			/* [retval][out] */ ILgWritingSystem **ppwseng) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddEngine(
			/* [in] */ ILgWritingSystem *pwseng) = 0;

		virtual HRESULT STDMETHODCALLTYPE RemoveEngine(
			/* [in] */ int ws) = 0;

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

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UnicodeCharProps(
			/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultCollater(
			/* [in] */ int ws,
			/* [retval][out] */ ILgCollatingEngine **ppcoleng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CharPropEngine(
			/* [in] */ int ws,
			/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Renderer(
			/* [in] */ int ws,
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppre) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RendererFromChrp(
			/* [out][in] */ LgCharRenderProps *pchrp,
			/* [retval][out] */ IRenderEngine **ppre) = 0;

		virtual HRESULT STDMETHODCALLTYPE Shutdown( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE SaveWritingSystems( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UserWs(
			/* [retval][out] */ int *pws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BypassInstall(
			/* [retval][out] */ ComBool *pfBypass) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_BypassInstall(
			/* [in] */ ComBool fBypass) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgWritingSystemFactoryVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgWritingSystemFactory * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *AddEngine )(
			ILgWritingSystemFactory * This,
			/* [in] */ ILgWritingSystem *pwseng);

		HRESULT ( STDMETHODCALLTYPE *RemoveEngine )(
			ILgWritingSystemFactory * This,
			/* [in] */ int ws);

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

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UnicodeCharProps )(
			ILgWritingSystemFactory * This,
			/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultCollater )(
			ILgWritingSystemFactory * This,
			/* [in] */ int ws,
			/* [retval][out] */ ILgCollatingEngine **ppcoleng);

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
			/* [out][in] */ LgCharRenderProps *pchrp,
			/* [retval][out] */ IRenderEngine **ppre);

		HRESULT ( STDMETHODCALLTYPE *Shutdown )(
			ILgWritingSystemFactory * This);

		HRESULT ( STDMETHODCALLTYPE *Clear )(
			ILgWritingSystemFactory * This);

		HRESULT ( STDMETHODCALLTYPE *SaveWritingSystems )(
			ILgWritingSystemFactory * This);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			ILgWritingSystemFactory * This,
			/* [in] */ IStorage *pstg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UserWs )(
			ILgWritingSystemFactory * This,
			/* [retval][out] */ int *pws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BypassInstall )(
			ILgWritingSystemFactory * This,
			/* [retval][out] */ ComBool *pfBypass);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_BypassInstall )(
			ILgWritingSystemFactory * This,
			/* [in] */ ComBool fBypass);

		END_INTERFACE
	} ILgWritingSystemFactoryVtbl;

	interface ILgWritingSystemFactory
	{
		CONST_VTBL struct ILgWritingSystemFactoryVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgWritingSystemFactory_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgWritingSystemFactory_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgWritingSystemFactory_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgWritingSystemFactory_get_Engine(This,bstrIcuLocale,ppwseng)	\
	(This)->lpVtbl -> get_Engine(This,bstrIcuLocale,ppwseng)

#define ILgWritingSystemFactory_get_EngineOrNull(This,ws,ppwseng)	\
	(This)->lpVtbl -> get_EngineOrNull(This,ws,ppwseng)

#define ILgWritingSystemFactory_AddEngine(This,pwseng)	\
	(This)->lpVtbl -> AddEngine(This,pwseng)

#define ILgWritingSystemFactory_RemoveEngine(This,ws)	\
	(This)->lpVtbl -> RemoveEngine(This,ws)

#define ILgWritingSystemFactory_GetWsFromStr(This,bstr,pwsId)	\
	(This)->lpVtbl -> GetWsFromStr(This,bstr,pwsId)

#define ILgWritingSystemFactory_GetStrFromWs(This,wsId,pbstr)	\
	(This)->lpVtbl -> GetStrFromWs(This,wsId,pbstr)

#define ILgWritingSystemFactory_get_NumberOfWs(This,pcws)	\
	(This)->lpVtbl -> get_NumberOfWs(This,pcws)

#define ILgWritingSystemFactory_GetWritingSystems(This,rgws,cws)	\
	(This)->lpVtbl -> GetWritingSystems(This,rgws,cws)

#define ILgWritingSystemFactory_get_UnicodeCharProps(This,pplcpe)	\
	(This)->lpVtbl -> get_UnicodeCharProps(This,pplcpe)

#define ILgWritingSystemFactory_get_DefaultCollater(This,ws,ppcoleng)	\
	(This)->lpVtbl -> get_DefaultCollater(This,ws,ppcoleng)

#define ILgWritingSystemFactory_get_CharPropEngine(This,ws,pplcpe)	\
	(This)->lpVtbl -> get_CharPropEngine(This,ws,pplcpe)

#define ILgWritingSystemFactory_get_Renderer(This,ws,pvg,ppre)	\
	(This)->lpVtbl -> get_Renderer(This,ws,pvg,ppre)

#define ILgWritingSystemFactory_get_RendererFromChrp(This,pchrp,ppre)	\
	(This)->lpVtbl -> get_RendererFromChrp(This,pchrp,ppre)

#define ILgWritingSystemFactory_Shutdown(This)	\
	(This)->lpVtbl -> Shutdown(This)

#define ILgWritingSystemFactory_Clear(This)	\
	(This)->lpVtbl -> Clear(This)

#define ILgWritingSystemFactory_SaveWritingSystems(This)	\
	(This)->lpVtbl -> SaveWritingSystems(This)

#define ILgWritingSystemFactory_Serialize(This,pstg)	\
	(This)->lpVtbl -> Serialize(This,pstg)

#define ILgWritingSystemFactory_get_UserWs(This,pws)	\
	(This)->lpVtbl -> get_UserWs(This,pws)

#define ILgWritingSystemFactory_get_BypassInstall(This,pfBypass)	\
	(This)->lpVtbl -> get_BypassInstall(This,pfBypass)

#define ILgWritingSystemFactory_put_BypassInstall(This,fBypass)	\
	(This)->lpVtbl -> put_BypassInstall(This,fBypass)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_Engine_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ BSTR bstrIcuLocale,
	/* [retval][out] */ ILgWritingSystem **ppwseng);


void __RPC_STUB ILgWritingSystemFactory_get_Engine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_EngineOrNull_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int ws,
	/* [retval][out] */ ILgWritingSystem **ppwseng);


void __RPC_STUB ILgWritingSystemFactory_get_EngineOrNull_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_AddEngine_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ ILgWritingSystem *pwseng);


void __RPC_STUB ILgWritingSystemFactory_AddEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_RemoveEngine_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int ws);


void __RPC_STUB ILgWritingSystemFactory_RemoveEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_GetWsFromStr_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ int *pwsId);


void __RPC_STUB ILgWritingSystemFactory_GetWsFromStr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_GetStrFromWs_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int wsId,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystemFactory_GetStrFromWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_NumberOfWs_Proxy(
	ILgWritingSystemFactory * This,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ILgWritingSystemFactory_get_NumberOfWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_GetWritingSystems_Proxy(
	ILgWritingSystemFactory * This,
	/* [size_is][out] */ int *rgws,
	/* [in] */ int cws);


void __RPC_STUB ILgWritingSystemFactory_GetWritingSystems_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_UnicodeCharProps_Proxy(
	ILgWritingSystemFactory * This,
	/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe);


void __RPC_STUB ILgWritingSystemFactory_get_UnicodeCharProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_DefaultCollater_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int ws,
	/* [retval][out] */ ILgCollatingEngine **ppcoleng);


void __RPC_STUB ILgWritingSystemFactory_get_DefaultCollater_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_CharPropEngine_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int ws,
	/* [retval][out] */ ILgCharacterPropertyEngine **pplcpe);


void __RPC_STUB ILgWritingSystemFactory_get_CharPropEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_Renderer_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ int ws,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ IRenderEngine **ppre);


void __RPC_STUB ILgWritingSystemFactory_get_Renderer_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_RendererFromChrp_Proxy(
	ILgWritingSystemFactory * This,
	/* [out][in] */ LgCharRenderProps *pchrp,
	/* [retval][out] */ IRenderEngine **ppre);


void __RPC_STUB ILgWritingSystemFactory_get_RendererFromChrp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_Shutdown_Proxy(
	ILgWritingSystemFactory * This);


void __RPC_STUB ILgWritingSystemFactory_Shutdown_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_Clear_Proxy(
	ILgWritingSystemFactory * This);


void __RPC_STUB ILgWritingSystemFactory_Clear_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_SaveWritingSystems_Proxy(
	ILgWritingSystemFactory * This);


void __RPC_STUB ILgWritingSystemFactory_SaveWritingSystems_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_Serialize_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgWritingSystemFactory_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_UserWs_Proxy(
	ILgWritingSystemFactory * This,
	/* [retval][out] */ int *pws);


void __RPC_STUB ILgWritingSystemFactory_get_UserWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_get_BypassInstall_Proxy(
	ILgWritingSystemFactory * This,
	/* [retval][out] */ ComBool *pfBypass);


void __RPC_STUB ILgWritingSystemFactory_get_BypassInstall_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystemFactory_put_BypassInstall_Proxy(
	ILgWritingSystemFactory * This,
	/* [in] */ ComBool fBypass);


void __RPC_STUB ILgWritingSystemFactory_put_BypassInstall_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgWritingSystemFactory_INTERFACE_DEFINED__ */


#ifndef __ITsStreamWrapper_INTERFACE_DEFINED__
#define __ITsStreamWrapper_INTERFACE_DEFINED__

/* interface ITsStreamWrapper */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ITsStreamWrapper;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("4516897E-314B-49d8-8378-F2E105C80009")
	ITsStreamWrapper : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Stream(
			/* [retval][out] */ IStream **ppstrm) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Contents(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Contents(
			/* [in] */ BSTR bstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteTssAsXml(
			/* [in] */ ITsString *ptss,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData) = 0;

		virtual HRESULT STDMETHODCALLTYPE ReadTssFromXml(
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [retval][out] */ ITsString **pptss) = 0;

	};

#else 	/* C style interface */

	typedef struct ITsStreamWrapperVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ITsStreamWrapper * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ITsStreamWrapper * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ITsStreamWrapper * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Stream )(
			ITsStreamWrapper * This,
			/* [retval][out] */ IStream **ppstrm);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Contents )(
			ITsStreamWrapper * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Contents )(
			ITsStreamWrapper * This,
			/* [in] */ BSTR bstr);

		HRESULT ( STDMETHODCALLTYPE *WriteTssAsXml )(
			ITsStreamWrapper * This,
			/* [in] */ ITsString *ptss,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int cchIndent,
			/* [in] */ int ws,
			/* [in] */ ComBool fWriteObjData);

		HRESULT ( STDMETHODCALLTYPE *ReadTssFromXml )(
			ITsStreamWrapper * This,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [retval][out] */ ITsString **pptss);

		END_INTERFACE
	} ITsStreamWrapperVtbl;

	interface ITsStreamWrapper
	{
		CONST_VTBL struct ITsStreamWrapperVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ITsStreamWrapper_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ITsStreamWrapper_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ITsStreamWrapper_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ITsStreamWrapper_get_Stream(This,ppstrm)	\
	(This)->lpVtbl -> get_Stream(This,ppstrm)

#define ITsStreamWrapper_get_Contents(This,pbstr)	\
	(This)->lpVtbl -> get_Contents(This,pbstr)

#define ITsStreamWrapper_put_Contents(This,bstr)	\
	(This)->lpVtbl -> put_Contents(This,bstr)

#define ITsStreamWrapper_WriteTssAsXml(This,ptss,pwsf,cchIndent,ws,fWriteObjData)	\
	(This)->lpVtbl -> WriteTssAsXml(This,ptss,pwsf,cchIndent,ws,fWriteObjData)

#define ITsStreamWrapper_ReadTssFromXml(This,pwsf,pptss)	\
	(This)->lpVtbl -> ReadTssFromXml(This,pwsf,pptss)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStreamWrapper_get_Stream_Proxy(
	ITsStreamWrapper * This,
	/* [retval][out] */ IStream **ppstrm);


void __RPC_STUB ITsStreamWrapper_get_Stream_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ITsStreamWrapper_get_Contents_Proxy(
	ITsStreamWrapper * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ITsStreamWrapper_get_Contents_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ITsStreamWrapper_put_Contents_Proxy(
	ITsStreamWrapper * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ITsStreamWrapper_put_Contents_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStreamWrapper_WriteTssAsXml_Proxy(
	ITsStreamWrapper * This,
	/* [in] */ ITsString *ptss,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [in] */ int cchIndent,
	/* [in] */ int ws,
	/* [in] */ ComBool fWriteObjData);


void __RPC_STUB ITsStreamWrapper_WriteTssAsXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ITsStreamWrapper_ReadTssFromXml_Proxy(
	ITsStreamWrapper * This,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ITsStreamWrapper_ReadTssFromXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ITsStreamWrapper_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_TsStrFactory;

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76E9-BE04-11d3-8D9A-005004DEFEC4")
TsStrFactory;
#endif

EXTERN_C const CLSID CLSID_TsPropsFactory;

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EA-BE04-11d3-8D9A-005004DEFEC4")
TsPropsFactory;
#endif

EXTERN_C const CLSID CLSID_TsStrBldr;

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EB-BE04-11d3-8D9A-005004DEFEC4")
TsStrBldr;
#endif

EXTERN_C const CLSID CLSID_TsIncStrBldr;

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76EC-BE04-11d3-8D9A-005004DEFEC4")
TsIncStrBldr;
#endif

EXTERN_C const CLSID CLSID_TsPropsBldr;

#ifdef __cplusplus

class DECLSPEC_UUID("F1EF76ED-BE04-11d3-8D9A-005004DEFEC4")
TsPropsBldr;
#endif

EXTERN_C const CLSID CLSID_TsMultiString;

#ifdef __cplusplus

class DECLSPEC_UUID("7A1B89C0-C2D6-11d3-9BB7-00400541F9E9")
TsMultiString;
#endif

EXTERN_C const CLSID CLSID_TsStreamWrapper;

#ifdef __cplusplus

class DECLSPEC_UUID("60A7A639-3774-43e8-AE40-D911EC8E3A35")
TsStreamWrapper;
#endif

#ifndef __ILgInputMethodEditor_INTERFACE_DEFINED__
#define __ILgInputMethodEditor_INTERFACE_DEFINED__

/* interface ILgInputMethodEditor */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgInputMethodEditor;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("17aebfe0-c00a-11d2-8078-0000c0fb81b5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgInputMethodEditor_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgInputMethodEditor_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgInputMethodEditor_Setup(This)	\
	(This)->lpVtbl -> Setup(This)

#define ILgInputMethodEditor_Replace(This,bstrInput,pttpInput,ptsbOld,ichMin,ichLim,pichModMin,pichModLim,pichIP)	\
	(This)->lpVtbl -> Replace(This,bstrInput,pttpInput,ptsbOld,ichMin,ichLim,pichModMin,pichModLim,pichIP)

#define ILgInputMethodEditor_Backspace(This,pichStart,cactBackspace,ptsbOld,pichModMin,pichModLim,pichIP,pcactBsRemaining)	\
	(This)->lpVtbl -> Backspace(This,pichStart,cactBackspace,ptsbOld,pichModMin,pichModLim,pichIP,pcactBsRemaining)

#define ILgInputMethodEditor_DeleteForward(This,pichStart,cactDelForward,ptsbInOut,pichModMin,pichModLim,pichIP,pcactDfRemaining)	\
	(This)->lpVtbl -> DeleteForward(This,pichStart,cactDelForward,ptsbInOut,pichModMin,pichModLim,pichIP,pcactDfRemaining)

#define ILgInputMethodEditor_IsValidInsertionPoint(This,ich,ptss,pfValid)	\
	(This)->lpVtbl -> IsValidInsertionPoint(This,ich,ptss,pfValid)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgInputMethodEditor_Setup_Proxy(
	ILgInputMethodEditor * This);


void __RPC_STUB ILgInputMethodEditor_Setup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted][local] */ HRESULT STDMETHODCALLTYPE ILgInputMethodEditor_Replace_Proxy(
	ILgInputMethodEditor * This,
	/* [in] */ BSTR bstrInput,
	/* [in] */ ITsTextProps *pttpInput,
	/* [in] */ ITsStrBldr *ptsbOld,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [out] */ int *pichModMin,
	/* [out] */ int *pichModLim,
	/* [out] */ int *pichIP);


void __RPC_STUB ILgInputMethodEditor_Replace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgInputMethodEditor_Backspace_Proxy(
	ILgInputMethodEditor * This,
	/* [in] */ int pichStart,
	/* [in] */ int cactBackspace,
	/* [in] */ ITsStrBldr *ptsbOld,
	/* [out] */ int *pichModMin,
	/* [out] */ int *pichModLim,
	/* [out] */ int *pichIP,
	/* [out] */ int *pcactBsRemaining);


void __RPC_STUB ILgInputMethodEditor_Backspace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgInputMethodEditor_DeleteForward_Proxy(
	ILgInputMethodEditor * This,
	/* [in] */ int pichStart,
	/* [in] */ int cactDelForward,
	/* [in] */ ITsStrBldr *ptsbInOut,
	/* [out] */ int *pichModMin,
	/* [out] */ int *pichModLim,
	/* [out] */ int *pichIP,
	/* [out] */ int *pcactDfRemaining);


void __RPC_STUB ILgInputMethodEditor_DeleteForward_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgInputMethodEditor_IsValidInsertionPoint_Proxy(
	ILgInputMethodEditor * This,
	/* [in] */ int ich,
	/* [in] */ ITsString *ptss,
	/* [retval][out] */ BOOL *pfValid);


void __RPC_STUB ILgInputMethodEditor_IsValidInsertionPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgInputMethodEditor_INTERFACE_DEFINED__ */


#ifndef __IVwGraphics_INTERFACE_DEFINED__
#define __IVwGraphics_INTERFACE_DEFINED__

/* interface IVwGraphics */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwGraphics;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("3A3CE0A1-B5EB-43bd-9C89-35EAA110F12B")
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
			/* [in] */ long x,
			/* [in] */ long y,
			/* [in] */ long cx,
			/* [in] */ long cy,
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
			/* [iid_is][out] */ void **ppvObject);

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
			/* [in] */ long x,
			/* [in] */ long y,
			/* [in] */ long cx,
			/* [in] */ long cy,
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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwGraphics_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwGraphics_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwGraphics_InvertRect(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> InvertRect(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphics_put_ForeColor(This,clr)	\
	(This)->lpVtbl -> put_ForeColor(This,clr)

#define IVwGraphics_put_BackColor(This,clr)	\
	(This)->lpVtbl -> put_BackColor(This,clr)

#define IVwGraphics_DrawRectangle(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> DrawRectangle(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphics_DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)	\
	(This)->lpVtbl -> DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)

#define IVwGraphics_DrawLine(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> DrawLine(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphics_DrawText(This,x,y,cch,prgch,xStretch)	\
	(This)->lpVtbl -> DrawText(This,x,y,cch,prgch,xStretch)

#define IVwGraphics_DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)	\
	(This)->lpVtbl -> DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)

#define IVwGraphics_GetTextExtent(This,cch,prgch,px,py)	\
	(This)->lpVtbl -> GetTextExtent(This,cch,prgch,px,py)

#define IVwGraphics_GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)	\
	(This)->lpVtbl -> GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)

#define IVwGraphics_GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)	\
	(This)->lpVtbl -> GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)

#define IVwGraphics_GetFontEmSquare(This,pxyFontEmSquare)	\
	(This)->lpVtbl -> GetFontEmSquare(This,pxyFontEmSquare)

#define IVwGraphics_GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)	\
	(This)->lpVtbl -> GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)

#define IVwGraphics_GetFontData(This,nTableId,pcbTableSz,pbstrTableData)	\
	(This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,pbstrTableData)

#define IVwGraphics_GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)	\
	(This)->lpVtbl -> GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)

#define IVwGraphics_XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)	\
	(This)->lpVtbl -> XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)

#define IVwGraphics_get_FontAscent(This,py)	\
	(This)->lpVtbl -> get_FontAscent(This,py)

#define IVwGraphics_get_FontDescent(This,pyRet)	\
	(This)->lpVtbl -> get_FontDescent(This,pyRet)

#define IVwGraphics_get_FontCharProperties(This,pchrp)	\
	(This)->lpVtbl -> get_FontCharProperties(This,pchrp)

#define IVwGraphics_ReleaseDC(This)	\
	(This)->lpVtbl -> ReleaseDC(This)

#define IVwGraphics_get_XUnitsPerInch(This,pxInch)	\
	(This)->lpVtbl -> get_XUnitsPerInch(This,pxInch)

#define IVwGraphics_put_XUnitsPerInch(This,xInch)	\
	(This)->lpVtbl -> put_XUnitsPerInch(This,xInch)

#define IVwGraphics_get_YUnitsPerInch(This,pyInch)	\
	(This)->lpVtbl -> get_YUnitsPerInch(This,pyInch)

#define IVwGraphics_put_YUnitsPerInch(This,yInch)	\
	(This)->lpVtbl -> put_YUnitsPerInch(This,yInch)

#define IVwGraphics_SetupGraphics(This,pchrp)	\
	(This)->lpVtbl -> SetupGraphics(This,pchrp)

#define IVwGraphics_PushClipRect(This,rcClip)	\
	(This)->lpVtbl -> PushClipRect(This,rcClip)

#define IVwGraphics_PopClipRect(This)	\
	(This)->lpVtbl -> PopClipRect(This)

#define IVwGraphics_DrawPolygon(This,cvpnt,prgvpnt)	\
	(This)->lpVtbl -> DrawPolygon(This,cvpnt,prgvpnt)

#define IVwGraphics_RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)	\
	(This)->lpVtbl -> RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)

#define IVwGraphics_MakePicture(This,pbData,cbData,pppic)	\
	(This)->lpVtbl -> MakePicture(This,pbData,cbData,pppic)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwGraphics_InvertRect_Proxy(
	IVwGraphics * This,
	/* [in] */ int xLeft,
	/* [in] */ int yTop,
	/* [in] */ int xRight,
	/* [in] */ int yBottom);


void __RPC_STUB IVwGraphics_InvertRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwGraphics_put_ForeColor_Proxy(
	IVwGraphics * This,
	/* [in] */ int clr);


void __RPC_STUB IVwGraphics_put_ForeColor_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwGraphics_put_BackColor_Proxy(
	IVwGraphics * This,
	/* [in] */ int clr);


void __RPC_STUB IVwGraphics_put_BackColor_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawRectangle_Proxy(
	IVwGraphics * This,
	/* [in] */ int xLeft,
	/* [in] */ int yTop,
	/* [in] */ int xRight,
	/* [in] */ int yBottom);


void __RPC_STUB IVwGraphics_DrawRectangle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawHorzLine_Proxy(
	IVwGraphics * This,
	/* [in] */ int xLeft,
	/* [in] */ int xRight,
	/* [in] */ int y,
	/* [in] */ int dyHeight,
	/* [in] */ int cdx,
	/* [size_is][in] */ int *prgdx,
	/* [out][in] */ int *pdxStart);


void __RPC_STUB IVwGraphics_DrawHorzLine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawLine_Proxy(
	IVwGraphics * This,
	/* [in] */ int xLeft,
	/* [in] */ int yTop,
	/* [in] */ int xRight,
	/* [in] */ int yBottom);


void __RPC_STUB IVwGraphics_DrawLine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawText_Proxy(
	IVwGraphics * This,
	/* [in] */ int x,
	/* [in] */ int y,
	/* [in] */ int cch,
	/* [size_is][in] */ const OLECHAR *prgch,
	/* [in] */ int xStretch);


void __RPC_STUB IVwGraphics_DrawText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawTextExt_Proxy(
	IVwGraphics * This,
	/* [in] */ int x,
	/* [in] */ int y,
	/* [in] */ int cch,
	/* [size_is][in] */ const OLECHAR *prgchw,
	/* [in] */ UINT uOptions,
	/* [in] */ const RECT *prect,
	/* [in] */ int *prgdx);


void __RPC_STUB IVwGraphics_DrawTextExt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetTextExtent_Proxy(
	IVwGraphics * This,
	/* [in] */ int cch,
	/* [size_is][in] */ const OLECHAR *prgch,
	/* [out] */ int *px,
	/* [out] */ int *py);


void __RPC_STUB IVwGraphics_GetTextExtent_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetTextLeadWidth_Proxy(
	IVwGraphics * This,
	/* [in] */ int cch,
	/* [size_is][in] */ const OLECHAR *prgch,
	/* [in] */ int ich,
	/* [in] */ int xStretch,
	/* [retval][out] */ int *px);


void __RPC_STUB IVwGraphics_GetTextLeadWidth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetClipRect_Proxy(
	IVwGraphics * This,
	/* [out] */ int *pxLeft,
	/* [out] */ int *pyTop,
	/* [out] */ int *pxRight,
	/* [out] */ int *pyBottom);


void __RPC_STUB IVwGraphics_GetClipRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetFontEmSquare_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ int *pxyFontEmSquare);


void __RPC_STUB IVwGraphics_GetFontEmSquare_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetGlyphMetrics_Proxy(
	IVwGraphics * This,
	/* [in] */ int chw,
	/* [out] */ int *psBoundingWidth,
	/* [out] */ int *pyBoundingHeight,
	/* [out] */ int *pxBoundingX,
	/* [out] */ int *pyBoundingY,
	/* [out] */ int *pxAdvanceX,
	/* [out] */ int *pyAdvanceY);


void __RPC_STUB IVwGraphics_GetGlyphMetrics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetFontData_Proxy(
	IVwGraphics * This,
	/* [in] */ int nTableId,
	/* [out] */ int *pcbTableSz,
	/* [retval][out] */ BSTR *pbstrTableData);


void __RPC_STUB IVwGraphics_GetFontData_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_GetFontDataRgch_Proxy(
	IVwGraphics * This,
	/* [in] */ int nTableId,
	/* [out] */ int *pcbTableSz,
	/* [size_is][out] */ OLECHAR *prgch,
	/* [in] */ int cchMax);


void __RPC_STUB IVwGraphics_GetFontDataRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_XYFromGlyphPoint_Proxy(
	IVwGraphics * This,
	/* [in] */ int chw,
	/* [in] */ int nPoint,
	/* [out] */ int *pxRet,
	/* [out] */ int *pyRet);


void __RPC_STUB IVwGraphics_XYFromGlyphPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwGraphics_get_FontAscent_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ int *py);


void __RPC_STUB IVwGraphics_get_FontAscent_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwGraphics_get_FontDescent_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ int *pyRet);


void __RPC_STUB IVwGraphics_get_FontDescent_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwGraphics_get_FontCharProperties_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ LgCharRenderProps *pchrp);


void __RPC_STUB IVwGraphics_get_FontCharProperties_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_ReleaseDC_Proxy(
	IVwGraphics * This);


void __RPC_STUB IVwGraphics_ReleaseDC_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwGraphics_get_XUnitsPerInch_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ int *pxInch);


void __RPC_STUB IVwGraphics_get_XUnitsPerInch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwGraphics_put_XUnitsPerInch_Proxy(
	IVwGraphics * This,
	/* [in] */ int xInch);


void __RPC_STUB IVwGraphics_put_XUnitsPerInch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwGraphics_get_YUnitsPerInch_Proxy(
	IVwGraphics * This,
	/* [retval][out] */ int *pyInch);


void __RPC_STUB IVwGraphics_get_YUnitsPerInch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwGraphics_put_YUnitsPerInch_Proxy(
	IVwGraphics * This,
	/* [in] */ int yInch);


void __RPC_STUB IVwGraphics_put_YUnitsPerInch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_SetupGraphics_Proxy(
	IVwGraphics * This,
	/* [out][in] */ LgCharRenderProps *pchrp);


void __RPC_STUB IVwGraphics_SetupGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_PushClipRect_Proxy(
	IVwGraphics * This,
	/* [in] */ RECT rcClip);


void __RPC_STUB IVwGraphics_PushClipRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_PopClipRect_Proxy(
	IVwGraphics * This);


void __RPC_STUB IVwGraphics_PopClipRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_DrawPolygon_Proxy(
	IVwGraphics * This,
	/* [in] */ int cvpnt,
	/* [size_is][in] */ POINT prgvpnt[  ]);


void __RPC_STUB IVwGraphics_DrawPolygon_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_RenderPicture_Proxy(
	IVwGraphics * This,
	/* [in] */ IPicture *ppic,
	/* [in] */ long x,
	/* [in] */ long y,
	/* [in] */ long cx,
	/* [in] */ long cy,
	/* [in] */ OLE_XPOS_HIMETRIC xSrc,
	/* [in] */ OLE_YPOS_HIMETRIC ySrc,
	/* [in] */ OLE_XSIZE_HIMETRIC cxSrc,
	/* [in] */ OLE_YSIZE_HIMETRIC cySrc,
	/* [in] */ LPCRECT prcWBounds);


void __RPC_STUB IVwGraphics_RenderPicture_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphics_MakePicture_Proxy(
	IVwGraphics * This,
	/* [size_is][in] */ byte *pbData,
	/* [in] */ int cbData,
	/* [retval][out] */ IPicture **pppic);


void __RPC_STUB IVwGraphics_MakePicture_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwGraphics_INTERFACE_DEFINED__ */


#ifndef __IJustifyingRenderer_INTERFACE_DEFINED__
#define __IJustifyingRenderer_INTERFACE_DEFINED__

/* interface IJustifyingRenderer */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IJustifyingRenderer;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("D7364EF2-43C0-4440-872A-336A4647B9A3")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IJustifyingRenderer_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IJustifyingRenderer_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IJustifyingRenderer_GetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,pValueRet)	\
	(This)->lpVtbl -> GetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,pValueRet)

#define IJustifyingRenderer_GetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,pValueRet)	\
	(This)->lpVtbl -> GetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,pValueRet)

#define IJustifyingRenderer_SetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,value)	\
	(This)->lpVtbl -> SetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,value)

#define IJustifyingRenderer_SetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,value)	\
	(This)->lpVtbl -> SetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,value)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IJustifyingRenderer_GetGlyphAttributeFloat_Proxy(
	IJustifyingRenderer * This,
	/* [in] */ int iGlyph,
	/* [in] */ int kjgatId,
	/* [in] */ int nLevel,
	/* [out] */ float *pValueRet);


void __RPC_STUB IJustifyingRenderer_GetGlyphAttributeFloat_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IJustifyingRenderer_GetGlyphAttributeInt_Proxy(
	IJustifyingRenderer * This,
	/* [in] */ int iGlyph,
	/* [in] */ int kjgatId,
	/* [in] */ int nLevel,
	/* [out] */ int *pValueRet);


void __RPC_STUB IJustifyingRenderer_GetGlyphAttributeInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IJustifyingRenderer_SetGlyphAttributeFloat_Proxy(
	IJustifyingRenderer * This,
	/* [in] */ int iGlyph,
	/* [in] */ int kjgatId,
	/* [in] */ int nLevel,
	/* [in] */ float value);


void __RPC_STUB IJustifyingRenderer_SetGlyphAttributeFloat_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IJustifyingRenderer_SetGlyphAttributeInt_Proxy(
	IJustifyingRenderer * This,
	/* [in] */ int iGlyph,
	/* [in] */ int kjgatId,
	/* [in] */ int nLevel,
	/* [in] */ int value);


void __RPC_STUB IJustifyingRenderer_SetGlyphAttributeInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IJustifyingRenderer_INTERFACE_DEFINED__ */


#ifndef __ISimpleInit_INTERFACE_DEFINED__
#define __ISimpleInit_INTERFACE_DEFINED__

/* interface ISimpleInit */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ISimpleInit;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FC1C0D0D-0483-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISimpleInit_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ISimpleInit_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ISimpleInit_InitNew(This,prgb,cb)	\
	(This)->lpVtbl -> InitNew(This,prgb,cb)

#define ISimpleInit_get_InitializationData(This,pbstr)	\
	(This)->lpVtbl -> get_InitializationData(This,pbstr)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [restricted] */ HRESULT STDMETHODCALLTYPE ISimpleInit_InitNew_Proxy(
	ISimpleInit * This,
	/* [size_is][in] */ const BYTE *prgb,
	/* [in] */ int cb);


void __RPC_STUB ISimpleInit_InitNew_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted][propget] */ HRESULT STDMETHODCALLTYPE ISimpleInit_get_InitializationData_Proxy(
	ISimpleInit * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ISimpleInit_get_InitializationData_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ISimpleInit_INTERFACE_DEFINED__ */


#ifndef __IVwGraphicsWin32_INTERFACE_DEFINED__
#define __IVwGraphicsWin32_INTERFACE_DEFINED__

/* interface IVwGraphicsWin32 */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwGraphicsWin32;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8E6828A3-8681-4822-B76D-6C4A25CAECE6")
	IVwGraphicsWin32 : public IVwGraphics
	{
	public:
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ HDC hdc) = 0;

		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetDeviceContext(
			/* [retval][out] */ HDC *phdc) = 0;

		virtual /* [local] */ HRESULT STDMETHODCALLTYPE SetMeasureDc(
			/* [in] */ HDC hdc) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetClipRect(
			/* [in] */ RECT *prcClip) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwGraphicsWin32Vtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwGraphicsWin32 * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [in] */ long x,
			/* [in] */ long y,
			/* [in] */ long cx,
			/* [in] */ long cy,
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

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Initialize )(
			IVwGraphicsWin32 * This,
			/* [in] */ HDC hdc);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *GetDeviceContext )(
			IVwGraphicsWin32 * This,
			/* [retval][out] */ HDC *phdc);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *SetMeasureDc )(
			IVwGraphicsWin32 * This,
			/* [in] */ HDC hdc);

		HRESULT ( STDMETHODCALLTYPE *SetClipRect )(
			IVwGraphicsWin32 * This,
			/* [in] */ RECT *prcClip);

		END_INTERFACE
	} IVwGraphicsWin32Vtbl;

	interface IVwGraphicsWin32
	{
		CONST_VTBL struct IVwGraphicsWin32Vtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwGraphicsWin32_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwGraphicsWin32_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwGraphicsWin32_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwGraphicsWin32_InvertRect(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> InvertRect(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphicsWin32_put_ForeColor(This,clr)	\
	(This)->lpVtbl -> put_ForeColor(This,clr)

#define IVwGraphicsWin32_put_BackColor(This,clr)	\
	(This)->lpVtbl -> put_BackColor(This,clr)

#define IVwGraphicsWin32_DrawRectangle(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> DrawRectangle(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphicsWin32_DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)	\
	(This)->lpVtbl -> DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)

#define IVwGraphicsWin32_DrawLine(This,xLeft,yTop,xRight,yBottom)	\
	(This)->lpVtbl -> DrawLine(This,xLeft,yTop,xRight,yBottom)

#define IVwGraphicsWin32_DrawText(This,x,y,cch,prgch,xStretch)	\
	(This)->lpVtbl -> DrawText(This,x,y,cch,prgch,xStretch)

#define IVwGraphicsWin32_DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)	\
	(This)->lpVtbl -> DrawTextExt(This,x,y,cch,prgchw,uOptions,prect,prgdx)

#define IVwGraphicsWin32_GetTextExtent(This,cch,prgch,px,py)	\
	(This)->lpVtbl -> GetTextExtent(This,cch,prgch,px,py)

#define IVwGraphicsWin32_GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)	\
	(This)->lpVtbl -> GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)

#define IVwGraphicsWin32_GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)	\
	(This)->lpVtbl -> GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)

#define IVwGraphicsWin32_GetFontEmSquare(This,pxyFontEmSquare)	\
	(This)->lpVtbl -> GetFontEmSquare(This,pxyFontEmSquare)

#define IVwGraphicsWin32_GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)	\
	(This)->lpVtbl -> GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)

#define IVwGraphicsWin32_GetFontData(This,nTableId,pcbTableSz,pbstrTableData)	\
	(This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,pbstrTableData)

#define IVwGraphicsWin32_GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)	\
	(This)->lpVtbl -> GetFontDataRgch(This,nTableId,pcbTableSz,prgch,cchMax)

#define IVwGraphicsWin32_XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)	\
	(This)->lpVtbl -> XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)

#define IVwGraphicsWin32_get_FontAscent(This,py)	\
	(This)->lpVtbl -> get_FontAscent(This,py)

#define IVwGraphicsWin32_get_FontDescent(This,pyRet)	\
	(This)->lpVtbl -> get_FontDescent(This,pyRet)

#define IVwGraphicsWin32_get_FontCharProperties(This,pchrp)	\
	(This)->lpVtbl -> get_FontCharProperties(This,pchrp)

#define IVwGraphicsWin32_ReleaseDC(This)	\
	(This)->lpVtbl -> ReleaseDC(This)

#define IVwGraphicsWin32_get_XUnitsPerInch(This,pxInch)	\
	(This)->lpVtbl -> get_XUnitsPerInch(This,pxInch)

#define IVwGraphicsWin32_put_XUnitsPerInch(This,xInch)	\
	(This)->lpVtbl -> put_XUnitsPerInch(This,xInch)

#define IVwGraphicsWin32_get_YUnitsPerInch(This,pyInch)	\
	(This)->lpVtbl -> get_YUnitsPerInch(This,pyInch)

#define IVwGraphicsWin32_put_YUnitsPerInch(This,yInch)	\
	(This)->lpVtbl -> put_YUnitsPerInch(This,yInch)

#define IVwGraphicsWin32_SetupGraphics(This,pchrp)	\
	(This)->lpVtbl -> SetupGraphics(This,pchrp)

#define IVwGraphicsWin32_PushClipRect(This,rcClip)	\
	(This)->lpVtbl -> PushClipRect(This,rcClip)

#define IVwGraphicsWin32_PopClipRect(This)	\
	(This)->lpVtbl -> PopClipRect(This)

#define IVwGraphicsWin32_DrawPolygon(This,cvpnt,prgvpnt)	\
	(This)->lpVtbl -> DrawPolygon(This,cvpnt,prgvpnt)

#define IVwGraphicsWin32_RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)	\
	(This)->lpVtbl -> RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)

#define IVwGraphicsWin32_MakePicture(This,pbData,cbData,pppic)	\
	(This)->lpVtbl -> MakePicture(This,pbData,cbData,pppic)


#define IVwGraphicsWin32_Initialize(This,hdc)	\
	(This)->lpVtbl -> Initialize(This,hdc)

#define IVwGraphicsWin32_GetDeviceContext(This,phdc)	\
	(This)->lpVtbl -> GetDeviceContext(This,phdc)

#define IVwGraphicsWin32_SetMeasureDc(This,hdc)	\
	(This)->lpVtbl -> SetMeasureDc(This,hdc)

#define IVwGraphicsWin32_SetClipRect(This,prcClip)	\
	(This)->lpVtbl -> SetClipRect(This,prcClip)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [local] */ HRESULT STDMETHODCALLTYPE IVwGraphicsWin32_Initialize_Proxy(
	IVwGraphicsWin32 * This,
	/* [in] */ HDC hdc);


void __RPC_STUB IVwGraphicsWin32_Initialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local] */ HRESULT STDMETHODCALLTYPE IVwGraphicsWin32_GetDeviceContext_Proxy(
	IVwGraphicsWin32 * This,
	/* [retval][out] */ HDC *phdc);


void __RPC_STUB IVwGraphicsWin32_GetDeviceContext_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [local] */ HRESULT STDMETHODCALLTYPE IVwGraphicsWin32_SetMeasureDc_Proxy(
	IVwGraphicsWin32 * This,
	/* [in] */ HDC hdc);


void __RPC_STUB IVwGraphicsWin32_SetMeasureDc_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwGraphicsWin32_SetClipRect_Proxy(
	IVwGraphicsWin32 * This,
	/* [in] */ RECT *prcClip);


void __RPC_STUB IVwGraphicsWin32_SetClipRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwGraphicsWin32_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_VwGraphicsWin32;

#ifdef __cplusplus

class DECLSPEC_UUID("D9F93A03-8F8F-4e1d-B001-F373C7651B66")
VwGraphicsWin32;
#endif

#ifndef __IVwTextSource_INTERFACE_DEFINED__
#define __IVwTextSource_INTERFACE_DEFINED__

/* interface IVwTextSource */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwTextSource;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("92AC8BE4-EDC8-11d3-8078-0000C0FB81B5")
	IVwTextSource : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Fetch(
			/* [in] */ int ichMin,
			/* [in] */ int ichLim,
			/* [size_is][out] */ OLECHAR *prgchBuf) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Length(
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

	};

#else 	/* C style interface */

	typedef struct IVwTextSourceVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwTextSource * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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

		END_INTERFACE
	} IVwTextSourceVtbl;

	interface IVwTextSource
	{
		CONST_VTBL struct IVwTextSourceVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwTextSource_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwTextSource_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwTextSource_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwTextSource_Fetch(This,ichMin,ichLim,prgchBuf)	\
	(This)->lpVtbl -> Fetch(This,ichMin,ichLim,prgchBuf)

#define IVwTextSource_get_Length(This,pcch)	\
	(This)->lpVtbl -> get_Length(This,pcch)

#define IVwTextSource_GetCharProps(This,ich,pchrp,pichMin,pichLim)	\
	(This)->lpVtbl -> GetCharProps(This,ich,pchrp,pichMin,pichLim)

#define IVwTextSource_GetParaProps(This,ich,pchrp,pichMin,pichLim)	\
	(This)->lpVtbl -> GetParaProps(This,ich,pchrp,pichMin,pichLim)

#define IVwTextSource_GetCharStringProp(This,ich,nId,pbstr,pichMin,pichLim)	\
	(This)->lpVtbl -> GetCharStringProp(This,ich,nId,pbstr,pichMin,pichLim)

#define IVwTextSource_GetParaStringProp(This,ich,nId,pbstr,pichMin,pichLim)	\
	(This)->lpVtbl -> GetParaStringProp(This,ich,nId,pbstr,pichMin,pichLim)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwTextSource_Fetch_Proxy(
	IVwTextSource * This,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [size_is][out] */ OLECHAR *prgchBuf);


void __RPC_STUB IVwTextSource_Fetch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwTextSource_get_Length_Proxy(
	IVwTextSource * This,
	/* [retval][out] */ int *pcch);


void __RPC_STUB IVwTextSource_get_Length_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwTextSource_GetCharProps_Proxy(
	IVwTextSource * This,
	/* [in] */ int ich,
	/* [out] */ LgCharRenderProps *pchrp,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB IVwTextSource_GetCharProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwTextSource_GetParaProps_Proxy(
	IVwTextSource * This,
	/* [in] */ int ich,
	/* [out] */ LgParaRenderProps *pchrp,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB IVwTextSource_GetParaProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwTextSource_GetCharStringProp_Proxy(
	IVwTextSource * This,
	/* [in] */ int ich,
	/* [in] */ int nId,
	/* [out] */ BSTR *pbstr,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB IVwTextSource_GetCharStringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwTextSource_GetParaStringProp_Proxy(
	IVwTextSource * This,
	/* [in] */ int ich,
	/* [in] */ int nId,
	/* [out] */ BSTR *pbstr,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB IVwTextSource_GetParaStringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwTextSource_INTERFACE_DEFINED__ */


#ifndef __IVwJustifier_INTERFACE_DEFINED__
#define __IVwJustifier_INTERFACE_DEFINED__

/* interface IVwJustifier */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwJustifier;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("BAC7725F-1D26-42b2-8E9D-8B9175782CC7")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwJustifier_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwJustifier_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwJustifier_AdjustGlyphWidths(This,pjren,iGlyphMin,iGlyphLim,dxCurrentWidth,dxDesiredWidth)	\
	(This)->lpVtbl -> AdjustGlyphWidths(This,pjren,iGlyphMin,iGlyphLim,dxCurrentWidth,dxDesiredWidth)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwJustifier_AdjustGlyphWidths_Proxy(
	IVwJustifier * This,
	/* [in] */ IJustifyingRenderer *pjren,
	/* [in] */ int iGlyphMin,
	/* [in] */ int iGlyphLim,
	/* [in] */ float dxCurrentWidth,
	/* [in] */ float dxDesiredWidth);


void __RPC_STUB IVwJustifier_AdjustGlyphWidths_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwJustifier_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_VwJustifier;

#ifdef __cplusplus

class DECLSPEC_UUID("B424D26F-6B8C-43c2-9DD4-C4A822764472")
VwJustifier;
#endif

#ifndef __ILgSegment_INTERFACE_DEFINED__
#define __ILgSegment_INTERFACE_DEFINED__

/* interface ILgSegment */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgSegment;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("7407F0FC-58B0-4476-A0C8-69431801E560")
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
			/* [out] */ ComBool *pfWeak) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetDirectionDepth(
			/* [in] */ int ichwBase,
			/* [in] */ int nNewDepth) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystem(
			/* [in] */ int ichBase,
			/* [out] */ int *pws) = 0;

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
			/* [in] */ ComBool bOn) = 0;

		virtual HRESULT STDMETHODCALLTYPE PositionOfRange(
			/* [in] */ int ichBase,
			/* [in] */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ int ichMin,
			/* [in] */ int ichim,
			/* [in] */ int ydTop,
			/* [in] */ int ydBottom,
			/* [in] */ RECT *rsBounds,
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

	};

#else 	/* C style interface */

	typedef struct ILgSegmentVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgSegment * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [out] */ ComBool *pfWeak);

		HRESULT ( STDMETHODCALLTYPE *SetDirectionDepth )(
			ILgSegment * This,
			/* [in] */ int ichwBase,
			/* [in] */ int nNewDepth);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystem )(
			ILgSegment * This,
			/* [in] */ int ichBase,
			/* [out] */ int *pws);

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
			/* [in] */ ComBool bOn);

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
			/* [in] */ RECT *rsBounds,
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

		END_INTERFACE
	} ILgSegmentVtbl;

	interface ILgSegment
	{
		CONST_VTBL struct ILgSegmentVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgSegment_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgSegment_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgSegment_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgSegment_DrawText(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)	\
	(This)->lpVtbl -> DrawText(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)

#define ILgSegment_Recompute(This,ichBase,pvg)	\
	(This)->lpVtbl -> Recompute(This,ichBase,pvg)

#define ILgSegment_get_Width(This,ichBase,pvg,px)	\
	(This)->lpVtbl -> get_Width(This,ichBase,pvg,px)

#define ILgSegment_get_RightOverhang(This,ichBase,pvg,px)	\
	(This)->lpVtbl -> get_RightOverhang(This,ichBase,pvg,px)

#define ILgSegment_get_LeftOverhang(This,ichBase,pvg,px)	\
	(This)->lpVtbl -> get_LeftOverhang(This,ichBase,pvg,px)

#define ILgSegment_get_Height(This,ichBase,pvg,py)	\
	(This)->lpVtbl -> get_Height(This,ichBase,pvg,py)

#define ILgSegment_get_Ascent(This,ichBase,pvg,py)	\
	(This)->lpVtbl -> get_Ascent(This,ichBase,pvg,py)

#define ILgSegment_Extent(This,ichBase,pvg,px,py)	\
	(This)->lpVtbl -> Extent(This,ichBase,pvg,px,py)

#define ILgSegment_BoundingRect(This,ichBase,pvg,rcSrc,rcDst,prcBounds)	\
	(This)->lpVtbl -> BoundingRect(This,ichBase,pvg,rcSrc,rcDst,prcBounds)

#define ILgSegment_GetActualWidth(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)	\
	(This)->lpVtbl -> GetActualWidth(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)

#define ILgSegment_get_AscentOverhang(This,ichBase,pvg,py)	\
	(This)->lpVtbl -> get_AscentOverhang(This,ichBase,pvg,py)

#define ILgSegment_get_DescentOverhang(This,ichBase,pvg,py)	\
	(This)->lpVtbl -> get_DescentOverhang(This,ichBase,pvg,py)

#define ILgSegment_get_RightToLeft(This,ichBase,pfResult)	\
	(This)->lpVtbl -> get_RightToLeft(This,ichBase,pfResult)

#define ILgSegment_get_DirectionDepth(This,ichBase,pnDepth,pfWeak)	\
	(This)->lpVtbl -> get_DirectionDepth(This,ichBase,pnDepth,pfWeak)

#define ILgSegment_SetDirectionDepth(This,ichwBase,nNewDepth)	\
	(This)->lpVtbl -> SetDirectionDepth(This,ichwBase,nNewDepth)

#define ILgSegment_get_WritingSystem(This,ichBase,pws)	\
	(This)->lpVtbl -> get_WritingSystem(This,ichBase,pws)

#define ILgSegment_get_Lim(This,ichBase,pdich)	\
	(This)->lpVtbl -> get_Lim(This,ichBase,pdich)

#define ILgSegment_get_LimInterest(This,ichBase,pdich)	\
	(This)->lpVtbl -> get_LimInterest(This,ichBase,pdich)

#define ILgSegment_put_EndLine(This,ichBase,pvg,fNewVal)	\
	(This)->lpVtbl -> put_EndLine(This,ichBase,pvg,fNewVal)

#define ILgSegment_put_StartLine(This,ichBase,pvg,fNewVal)	\
	(This)->lpVtbl -> put_StartLine(This,ichBase,pvg,fNewVal)

#define ILgSegment_get_StartBreakWeight(This,ichBase,pvg,plb)	\
	(This)->lpVtbl -> get_StartBreakWeight(This,ichBase,pvg,plb)

#define ILgSegment_get_EndBreakWeight(This,ichBase,pvg,plb)	\
	(This)->lpVtbl -> get_EndBreakWeight(This,ichBase,pvg,plb)

#define ILgSegment_get_Stretch(This,ichBase,pxs)	\
	(This)->lpVtbl -> get_Stretch(This,ichBase,pxs)

#define ILgSegment_put_Stretch(This,ichBase,xs)	\
	(This)->lpVtbl -> put_Stretch(This,ichBase,xs)

#define ILgSegment_IsValidInsertionPoint(This,ichBase,pvg,ich,pipvr)	\
	(This)->lpVtbl -> IsValidInsertionPoint(This,ichBase,pvg,ich,pipvr)

#define ILgSegment_DoBoundariesCoincide(This,ichBase,pvg,fBoundaryEnd,fBoundaryRight,pfResult)	\
	(This)->lpVtbl -> DoBoundariesCoincide(This,ichBase,pvg,fBoundaryEnd,fBoundaryRight,pfResult)

#define ILgSegment_DrawInsertionPoint(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,fOn,dm)	\
	(This)->lpVtbl -> DrawInsertionPoint(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,fOn,dm)

#define ILgSegment_PositionsOfIP(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,dm,rectPrimary,rectSecondary,pfPrimaryHere,pfSecHere)	\
	(This)->lpVtbl -> PositionsOfIP(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,dm,rectPrimary,rectSecondary,pfPrimaryHere,pfSecHere)

#define ILgSegment_DrawRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichLim,ydTop,ydBottom,bOn)	\
	(This)->lpVtbl -> DrawRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichLim,ydTop,ydBottom,bOn)

#define ILgSegment_PositionOfRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichim,ydTop,ydBottom,rsBounds,pfAnythingToDraw)	\
	(This)->lpVtbl -> PositionOfRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichim,ydTop,ydBottom,rsBounds,pfAnythingToDraw)

#define ILgSegment_PointToChar(This,ichBase,pvg,rcSrc,rcDst,ptdClickPosition,pich,pfAssocPrev)	\
	(This)->lpVtbl -> PointToChar(This,ichBase,pvg,rcSrc,rcDst,ptdClickPosition,pich,pfAssocPrev)

#define ILgSegment_ArrowKeyPosition(This,ichBase,pvg,pich,pfAssocPrev,fRight,fMovingIn,pfResult)	\
	(This)->lpVtbl -> ArrowKeyPosition(This,ichBase,pvg,pich,pfAssocPrev,fRight,fMovingIn,pfResult)

#define ILgSegment_ExtendSelectionPosition(This,ichBase,pvg,pich,fAssocPrevMatch,fAssocPrevNeeded,ichAnchor,fRight,fMovingIn,pfRet)	\
	(This)->lpVtbl -> ExtendSelectionPosition(This,ichBase,pvg,pich,fAssocPrevMatch,fAssocPrevNeeded,ichAnchor,fRight,fMovingIn,pfRet)

#define ILgSegment_GetCharPlacement(This,ichBase,pvg,ichMin,ichLim,rcSrc,rcDst,fSkipSpace,cxdMax,pcxd,prgxdLefts,prgxdRights,prgydUnderTops)	\
	(This)->lpVtbl -> GetCharPlacement(This,ichBase,pvg,ichMin,ichLim,rcSrc,rcDst,fSkipSpace,cxdMax,pcxd,prgxdLefts,prgxdRights,prgydUnderTops)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgSegment_DrawText_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [out] */ int *dxdWidth);


void __RPC_STUB ILgSegment_DrawText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_Recompute_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg);


void __RPC_STUB ILgSegment_Recompute_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_Width_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *px);


void __RPC_STUB ILgSegment_get_Width_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_RightOverhang_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *px);


void __RPC_STUB ILgSegment_get_RightOverhang_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_LeftOverhang_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *px);


void __RPC_STUB ILgSegment_get_LeftOverhang_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_Height_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *py);


void __RPC_STUB ILgSegment_get_Height_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_Ascent_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *py);


void __RPC_STUB ILgSegment_get_Ascent_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_Extent_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [out] */ int *px,
	/* [out] */ int *py);


void __RPC_STUB ILgSegment_Extent_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_BoundingRect_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [retval][out] */ RECT *prcBounds);


void __RPC_STUB ILgSegment_BoundingRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_GetActualWidth_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [out] */ int *dxdWidth);


void __RPC_STUB ILgSegment_GetActualWidth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_AscentOverhang_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *py);


void __RPC_STUB ILgSegment_get_AscentOverhang_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_DescentOverhang_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ int *py);


void __RPC_STUB ILgSegment_get_DescentOverhang_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_RightToLeft_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [retval][out] */ ComBool *pfResult);


void __RPC_STUB ILgSegment_get_RightToLeft_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_DirectionDepth_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [out] */ int *pnDepth,
	/* [out] */ ComBool *pfWeak);


void __RPC_STUB ILgSegment_get_DirectionDepth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_SetDirectionDepth_Proxy(
	ILgSegment * This,
	/* [in] */ int ichwBase,
	/* [in] */ int nNewDepth);


void __RPC_STUB ILgSegment_SetDirectionDepth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_WritingSystem_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [out] */ int *pws);


void __RPC_STUB ILgSegment_get_WritingSystem_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_Lim_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [retval][out] */ int *pdich);


void __RPC_STUB ILgSegment_get_Lim_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_LimInterest_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [retval][out] */ int *pdich);


void __RPC_STUB ILgSegment_get_LimInterest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgSegment_put_EndLine_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ ComBool fNewVal);


void __RPC_STUB ILgSegment_put_EndLine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgSegment_put_StartLine_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ ComBool fNewVal);


void __RPC_STUB ILgSegment_put_StartLine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_StartBreakWeight_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ LgLineBreak *plb);


void __RPC_STUB ILgSegment_get_StartBreakWeight_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_EndBreakWeight_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ LgLineBreak *plb);


void __RPC_STUB ILgSegment_get_EndBreakWeight_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSegment_get_Stretch_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [retval][out] */ int *pxs);


void __RPC_STUB ILgSegment_get_Stretch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgSegment_put_Stretch_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ int xs);


void __RPC_STUB ILgSegment_put_Stretch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_IsValidInsertionPoint_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ int ich,
	/* [retval][out] */ LgIpValidResult *pipvr);


void __RPC_STUB ILgSegment_IsValidInsertionPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_DoBoundariesCoincide_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ ComBool fBoundaryEnd,
	/* [in] */ ComBool fBoundaryRight,
	/* [retval][out] */ ComBool *pfResult);


void __RPC_STUB ILgSegment_DoBoundariesCoincide_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_DrawInsertionPoint_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ int ich,
	/* [in] */ ComBool fAssocPrev,
	/* [in] */ ComBool fOn,
	/* [in] */ LgIPDrawMode dm);


void __RPC_STUB ILgSegment_DrawInsertionPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_PositionsOfIP_Proxy(
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


void __RPC_STUB ILgSegment_PositionsOfIP_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_DrawRange_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [in] */ int ydTop,
	/* [in] */ int ydBottom,
	/* [in] */ ComBool bOn);


void __RPC_STUB ILgSegment_DrawRange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_PositionOfRange_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ int ichMin,
	/* [in] */ int ichim,
	/* [in] */ int ydTop,
	/* [in] */ int ydBottom,
	/* [in] */ RECT *rsBounds,
	/* [retval][out] */ ComBool *pfAnythingToDraw);


void __RPC_STUB ILgSegment_PositionOfRange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_PointToChar_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ POINT ptdClickPosition,
	/* [out] */ int *pich,
	/* [out] */ ComBool *pfAssocPrev);


void __RPC_STUB ILgSegment_PointToChar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_ArrowKeyPosition_Proxy(
	ILgSegment * This,
	/* [in] */ int ichBase,
	/* [in] */ IVwGraphics *pvg,
	/* [out][in] */ int *pich,
	/* [out][in] */ ComBool *pfAssocPrev,
	/* [in] */ ComBool fRight,
	/* [in] */ ComBool fMovingIn,
	/* [out] */ ComBool *pfResult);


void __RPC_STUB ILgSegment_ArrowKeyPosition_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_ExtendSelectionPosition_Proxy(
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


void __RPC_STUB ILgSegment_ExtendSelectionPosition_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSegment_GetCharPlacement_Proxy(
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


void __RPC_STUB ILgSegment_GetCharPlacement_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgSegment_INTERFACE_DEFINED__ */


#ifndef __IRenderEngine_INTERFACE_DEFINED__
#define __IRenderEngine_INTERFACE_DEFINED__

/* interface IRenderEngine */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IRenderEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("93CB892F-16D1-4dca-9C71-2E804BC9395C")
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

		virtual HRESULT STDMETHODCALLTYPE InterpretChrp(
			/* [out][in] */ LgCharRenderProps *pchrp) = 0;

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
			/* [iid_is][out] */ void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *InterpretChrp )(
			IRenderEngine * This,
			/* [out][in] */ LgCharRenderProps *pchrp);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRenderEngine_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IRenderEngine_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IRenderEngine_InitRenderer(This,pvg,bstrData)	\
	(This)->lpVtbl -> InitRenderer(This,pvg,bstrData)

#define IRenderEngine_FontIsValid(This)	\
	(This)->lpVtbl -> FontIsValid(This)

#define IRenderEngine_get_SegDatMaxLength(This,cb)	\
	(This)->lpVtbl -> get_SegDatMaxLength(This,cb)

#define IRenderEngine_FindBreakPoint(This,pvg,pts,pvjus,ichMin,ichLim,ichLimBacktrack,fNeedFinalBreak,fStartLine,dxMaxWidth,lbPref,lbMax,twsh,fParaRightToLeft,ppsegRet,pdichLimSeg,pdxWidth,pest,psegPrev)	\
	(This)->lpVtbl -> FindBreakPoint(This,pvg,pts,pvjus,ichMin,ichLim,ichLimBacktrack,fNeedFinalBreak,fStartLine,dxMaxWidth,lbPref,lbMax,twsh,fParaRightToLeft,ppsegRet,pdichLimSeg,pdxWidth,pest,psegPrev)

#define IRenderEngine_get_ScriptDirection(This,pgrfsdc)	\
	(This)->lpVtbl -> get_ScriptDirection(This,pgrfsdc)

#define IRenderEngine_get_ClassId(This,pguid)	\
	(This)->lpVtbl -> get_ClassId(This,pguid)

#define IRenderEngine_InterpretChrp(This,pchrp)	\
	(This)->lpVtbl -> InterpretChrp(This,pchrp)

#define IRenderEngine_get_WritingSystemFactory(This,ppwsf)	\
	(This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf)

#define IRenderEngine_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IRenderEngine_InitRenderer_Proxy(
	IRenderEngine * This,
	/* [in] */ IVwGraphics *pvg,
	/* [in] */ BSTR bstrData);


void __RPC_STUB IRenderEngine_InitRenderer_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderEngine_FontIsValid_Proxy(
	IRenderEngine * This);


void __RPC_STUB IRenderEngine_FontIsValid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRenderEngine_get_SegDatMaxLength_Proxy(
	IRenderEngine * This,
	/* [retval][out] */ int *cb);


void __RPC_STUB IRenderEngine_get_SegDatMaxLength_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderEngine_FindBreakPoint_Proxy(
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


void __RPC_STUB IRenderEngine_FindBreakPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRenderEngine_get_ScriptDirection_Proxy(
	IRenderEngine * This,
	/* [retval][out] */ int *pgrfsdc);


void __RPC_STUB IRenderEngine_get_ScriptDirection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRenderEngine_get_ClassId_Proxy(
	IRenderEngine * This,
	/* [retval][out] */ GUID *pguid);


void __RPC_STUB IRenderEngine_get_ClassId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderEngine_InterpretChrp_Proxy(
	IRenderEngine * This,
	/* [out][in] */ LgCharRenderProps *pchrp);


void __RPC_STUB IRenderEngine_InterpretChrp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRenderEngine_get_WritingSystemFactory_Proxy(
	IRenderEngine * This,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB IRenderEngine_get_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IRenderEngine_putref_WritingSystemFactory_Proxy(
	IRenderEngine * This,
	/* [in] */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB IRenderEngine_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IRenderEngine_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_RomRenderEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("ECB3CBB1-BD20-4d85-85B4-E2ABA245933B")
RomRenderEngine;
#endif

EXTERN_C const CLSID CLSID_UniscribeEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("B13AAFCD-F82C-4e9e-B414-5F8EBBE48773")
UniscribeEngine;
#endif

EXTERN_C const CLSID CLSID_FwGrEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("171E329C-7473-413c-959A-A8963297DA9C")
FwGrEngine;
#endif

#ifndef __IRenderingFeatures_INTERFACE_DEFINED__
#define __IRenderingFeatures_INTERFACE_DEFINED__

/* interface IRenderingFeatures */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IRenderingFeatures;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0A439F99-7BF2-4e11-A871-8AFAEB2B7D53")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRenderingFeatures_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IRenderingFeatures_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IRenderingFeatures_GetFeatureIDs(This,cMax,prgFids,pcfid)	\
	(This)->lpVtbl -> GetFeatureIDs(This,cMax,prgFids,pcfid)

#define IRenderingFeatures_GetFeatureLabel(This,fid,nLanguage,pbstrLabel)	\
	(This)->lpVtbl -> GetFeatureLabel(This,fid,nLanguage,pbstrLabel)

#define IRenderingFeatures_GetFeatureValues(This,fid,cfvalMax,prgfval,pcfval,pfvalDefault)	\
	(This)->lpVtbl -> GetFeatureValues(This,fid,cfvalMax,prgfval,pcfval,pfvalDefault)

#define IRenderingFeatures_GetFeatureValueLabel(This,fid,fval,nLanguage,pbstrLabel)	\
	(This)->lpVtbl -> GetFeatureValueLabel(This,fid,fval,nLanguage,pbstrLabel)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IRenderingFeatures_GetFeatureIDs_Proxy(
	IRenderingFeatures * This,
	/* [in] */ int cMax,
	/* [size_is][out] */ int *prgFids,
	/* [out] */ int *pcfid);


void __RPC_STUB IRenderingFeatures_GetFeatureIDs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderingFeatures_GetFeatureLabel_Proxy(
	IRenderingFeatures * This,
	/* [in] */ int fid,
	/* [in] */ int nLanguage,
	/* [out] */ BSTR *pbstrLabel);


void __RPC_STUB IRenderingFeatures_GetFeatureLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderingFeatures_GetFeatureValues_Proxy(
	IRenderingFeatures * This,
	/* [in] */ int fid,
	/* [in] */ int cfvalMax,
	/* [size_is][out] */ int *prgfval,
	/* [out] */ int *pcfval,
	/* [out] */ int *pfvalDefault);


void __RPC_STUB IRenderingFeatures_GetFeatureValues_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRenderingFeatures_GetFeatureValueLabel_Proxy(
	IRenderingFeatures * This,
	/* [in] */ int fid,
	/* [in] */ int fval,
	/* [in] */ int nLanguage,
	/* [out] */ BSTR *pbstrLabel);


void __RPC_STUB IRenderingFeatures_GetFeatureValueLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IRenderingFeatures_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwGraphiteProcess;

#ifdef __cplusplus

class DECLSPEC_UUID("CFB22792-6473-4017-815C-83DF93FF43BE")
FwGraphiteProcess;
#endif

#ifndef __ILgCharacterPropertyEngine_INTERFACE_DEFINED__
#define __ILgCharacterPropertyEngine_INTERFACE_DEFINED__

/* interface ILgCharacterPropertyEngine */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgCharacterPropertyEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("7C8B7F40-40C8-47f7-B10B-45372415778D")
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

		virtual HRESULT STDMETHODCALLTYPE NormalizeKd(
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE NormalizeKdRgch(
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
			/* [iid_is][out] */ void **ppvObject);

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

		HRESULT ( STDMETHODCALLTYPE *NormalizeKd )(
			ILgCharacterPropertyEngine * This,
			/* [in] */ BSTR bstr,
			/* [retval][out] */ BSTR *pbstr);

		/* [restricted] */ HRESULT ( STDMETHODCALLTYPE *NormalizeKdRgch )(
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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgCharacterPropertyEngine_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgCharacterPropertyEngine_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgCharacterPropertyEngine_get_GeneralCategory(This,ch,pcc)	\
	(This)->lpVtbl -> get_GeneralCategory(This,ch,pcc)

#define ILgCharacterPropertyEngine_get_BidiCategory(This,ch,pbic)	\
	(This)->lpVtbl -> get_BidiCategory(This,ch,pbic)

#define ILgCharacterPropertyEngine_get_IsLetter(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsLetter(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsWordForming(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsWordForming(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsPunctuation(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsPunctuation(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsNumber(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsNumber(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsSeparator(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsSeparator(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsSymbol(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsSymbol(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsMark(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsMark(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsOther(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsOther(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsUpper(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsUpper(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsLower(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsLower(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsTitle(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsTitle(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsModifier(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsModifier(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsOtherLetter(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsOtherLetter(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsOpen(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsOpen(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsClose(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsClose(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsWordMedial(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsWordMedial(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_IsControl(This,ch,pfRet)	\
	(This)->lpVtbl -> get_IsControl(This,ch,pfRet)

#define ILgCharacterPropertyEngine_get_ToLowerCh(This,ch,pch)	\
	(This)->lpVtbl -> get_ToLowerCh(This,ch,pch)

#define ILgCharacterPropertyEngine_get_ToUpperCh(This,ch,pch)	\
	(This)->lpVtbl -> get_ToUpperCh(This,ch,pch)

#define ILgCharacterPropertyEngine_get_ToTitleCh(This,ch,pch)	\
	(This)->lpVtbl -> get_ToTitleCh(This,ch,pch)

#define ILgCharacterPropertyEngine_ToLower(This,bstr,pbstr)	\
	(This)->lpVtbl -> ToLower(This,bstr,pbstr)

#define ILgCharacterPropertyEngine_ToUpper(This,bstr,pbstr)	\
	(This)->lpVtbl -> ToUpper(This,bstr,pbstr)

#define ILgCharacterPropertyEngine_ToTitle(This,bstr,pbstr)	\
	(This)->lpVtbl -> ToTitle(This,bstr,pbstr)

#define ILgCharacterPropertyEngine_ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	(This)->lpVtbl -> ToLowerRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)

#define ILgCharacterPropertyEngine_ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	(This)->lpVtbl -> ToUpperRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)

#define ILgCharacterPropertyEngine_ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)	\
	(This)->lpVtbl -> ToTitleRgch(This,prgchIn,cchIn,prgchOut,cchOut,pcchRet)

#define ILgCharacterPropertyEngine_get_IsUserDefinedClass(This,ch,chClass,pfRet)	\
	(This)->lpVtbl -> get_IsUserDefinedClass(This,ch,chClass,pfRet)

#define ILgCharacterPropertyEngine_get_SoundAlikeKey(This,bstrValue,pbstrKey)	\
	(This)->lpVtbl -> get_SoundAlikeKey(This,bstrValue,pbstrKey)

#define ILgCharacterPropertyEngine_get_CharacterName(This,ch,pbstrName)	\
	(This)->lpVtbl -> get_CharacterName(This,ch,pbstrName)

#define ILgCharacterPropertyEngine_get_Decomposition(This,ch,pbstr)	\
	(This)->lpVtbl -> get_Decomposition(This,ch,pbstr)

#define ILgCharacterPropertyEngine_DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	(This)->lpVtbl -> DecompositionRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)

#define ILgCharacterPropertyEngine_get_FullDecomp(This,ch,pbstrOut)	\
	(This)->lpVtbl -> get_FullDecomp(This,ch,pbstrOut)

#define ILgCharacterPropertyEngine_FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)	\
	(This)->lpVtbl -> FullDecompRgch(This,ch,cchMax,prgch,pcch,pfHasDecomp)

#define ILgCharacterPropertyEngine_get_NumericValue(This,ch,pn)	\
	(This)->lpVtbl -> get_NumericValue(This,ch,pn)

#define ILgCharacterPropertyEngine_get_CombiningClass(This,ch,pn)	\
	(This)->lpVtbl -> get_CombiningClass(This,ch,pn)

#define ILgCharacterPropertyEngine_get_Comment(This,ch,pbstr)	\
	(This)->lpVtbl -> get_Comment(This,ch,pbstr)

#define ILgCharacterPropertyEngine_GetLineBreakProps(This,prgchIn,cchIn,prglbOut)	\
	(This)->lpVtbl -> GetLineBreakProps(This,prgchIn,cchIn,prglbOut)

#define ILgCharacterPropertyEngine_GetLineBreakStatus(This,prglbpIn,cb,prglbsOut)	\
	(This)->lpVtbl -> GetLineBreakStatus(This,prglbpIn,cb,prglbsOut)

#define ILgCharacterPropertyEngine_GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak)	\
	(This)->lpVtbl -> GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak)

#define ILgCharacterPropertyEngine_StripDiacritics(This,bstr,pbstr)	\
	(This)->lpVtbl -> StripDiacritics(This,bstr,pbstr)

#define ILgCharacterPropertyEngine_StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	(This)->lpVtbl -> StripDiacriticsRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)

#define ILgCharacterPropertyEngine_NormalizeKd(This,bstr,pbstr)	\
	(This)->lpVtbl -> NormalizeKd(This,bstr,pbstr)

#define ILgCharacterPropertyEngine_NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)	\
	(This)->lpVtbl -> NormalizeKdRgch(This,prgchIn,cchIn,prgchOut,cchMaxOut,pcchOut)

#define ILgCharacterPropertyEngine_get_Locale(This,pnLocale)	\
	(This)->lpVtbl -> get_Locale(This,pnLocale)

#define ILgCharacterPropertyEngine_put_Locale(This,nLocale)	\
	(This)->lpVtbl -> put_Locale(This,nLocale)

#define ILgCharacterPropertyEngine_GetLineBreakText(This,cchMax,prgchOut,pcchOut)	\
	(This)->lpVtbl -> GetLineBreakText(This,cchMax,prgchOut,pcchOut)

#define ILgCharacterPropertyEngine_put_LineBreakText(This,prgchIn,cchMax)	\
	(This)->lpVtbl -> put_LineBreakText(This,prgchIn,cchMax)

#define ILgCharacterPropertyEngine_LineBreakBefore(This,ichIn,pichOut,plbWeight)	\
	(This)->lpVtbl -> LineBreakBefore(This,ichIn,pichOut,plbWeight)

#define ILgCharacterPropertyEngine_LineBreakAfter(This,ichIn,pichOut,plbWeight)	\
	(This)->lpVtbl -> LineBreakAfter(This,ichIn,pichOut,plbWeight)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_GeneralCategory_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ LgGeneralCharCategory *pcc);


void __RPC_STUB ILgCharacterPropertyEngine_get_GeneralCategory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_BidiCategory_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ LgBidiCategory *pbic);


void __RPC_STUB ILgCharacterPropertyEngine_get_BidiCategory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsLetter_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsLetter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsWordForming_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsWordForming_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsPunctuation_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsPunctuation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsNumber_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsNumber_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsSeparator_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsSeparator_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsSymbol_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsSymbol_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsMark_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsMark_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsOther_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsOther_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsUpper_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsUpper_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsLower_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsLower_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsTitle_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsTitle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsModifier_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsModifier_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsOtherLetter_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsOtherLetter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsOpen_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsOpen_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsClose_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsClose_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsWordMedial_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsWordMedial_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsControl_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsControl_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_ToLowerCh_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ int *pch);


void __RPC_STUB ILgCharacterPropertyEngine_get_ToLowerCh_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_ToUpperCh_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ int *pch);


void __RPC_STUB ILgCharacterPropertyEngine_get_ToUpperCh_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_ToTitleCh_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ int *pch);


void __RPC_STUB ILgCharacterPropertyEngine_get_ToTitleCh_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToLower_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_ToLower_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToUpper_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_ToUpper_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToTitle_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_ToTitle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToLowerRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [in] */ int cchOut,
	/* [out] */ int *pcchRet);


void __RPC_STUB ILgCharacterPropertyEngine_ToLowerRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToUpperRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [in] */ int cchOut,
	/* [out] */ int *pcchRet);


void __RPC_STUB ILgCharacterPropertyEngine_ToUpperRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_ToTitleRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [in] */ int cchOut,
	/* [out] */ int *pcchRet);


void __RPC_STUB ILgCharacterPropertyEngine_ToTitleRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_IsUserDefinedClass_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [in] */ int chClass,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB ILgCharacterPropertyEngine_get_IsUserDefinedClass_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_SoundAlikeKey_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstrValue,
	/* [retval][out] */ BSTR *pbstrKey);


void __RPC_STUB ILgCharacterPropertyEngine_get_SoundAlikeKey_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_CharacterName_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgCharacterPropertyEngine_get_CharacterName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_Decomposition_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_get_Decomposition_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_DecompositionRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [in] */ int cchMax,
	/* [out] */ OLECHAR *prgch,
	/* [out] */ int *pcch,
	/* [out] */ ComBool *pfHasDecomp);


void __RPC_STUB ILgCharacterPropertyEngine_DecompositionRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_FullDecomp_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ BSTR *pbstrOut);


void __RPC_STUB ILgCharacterPropertyEngine_get_FullDecomp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_FullDecompRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [in] */ int cchMax,
	/* [out] */ OLECHAR *prgch,
	/* [out] */ int *pcch,
	/* [out] */ ComBool *pfHasDecomp);


void __RPC_STUB ILgCharacterPropertyEngine_FullDecompRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_NumericValue_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ int *pn);


void __RPC_STUB ILgCharacterPropertyEngine_get_NumericValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_CombiningClass_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ int *pn);


void __RPC_STUB ILgCharacterPropertyEngine_get_CombiningClass_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_Comment_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ch,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_get_Comment_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_GetLineBreakProps_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ const OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ byte *prglbOut);


void __RPC_STUB ILgCharacterPropertyEngine_GetLineBreakProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_GetLineBreakStatus_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ const byte *prglbpIn,
	/* [in] */ int cb,
	/* [size_is][out] */ byte *prglbsOut);


void __RPC_STUB ILgCharacterPropertyEngine_GetLineBreakStatus_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_GetLineBreakInfo_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ const OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [in] */ int ichMin,
	/* [in] */ int ichLim,
	/* [size_is][out] */ byte *prglbsOut,
	/* [out] */ int *pichBreak);


void __RPC_STUB ILgCharacterPropertyEngine_GetLineBreakInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_StripDiacritics_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_StripDiacritics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_StripDiacriticsRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [in] */ int cchMaxOut,
	/* [out] */ int *pcchOut);


void __RPC_STUB ILgCharacterPropertyEngine_StripDiacriticsRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_NormalizeKd_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCharacterPropertyEngine_NormalizeKd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_NormalizeKdRgch_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchIn,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [in] */ int cchMaxOut,
	/* [out] */ int *pcchOut);


void __RPC_STUB ILgCharacterPropertyEngine_NormalizeKdRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_get_Locale_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [retval][out] */ int *pnLocale);


void __RPC_STUB ILgCharacterPropertyEngine_get_Locale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_put_Locale_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int nLocale);


void __RPC_STUB ILgCharacterPropertyEngine_put_Locale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_GetLineBreakText_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int cchMax,
	/* [out] */ OLECHAR *prgchOut,
	/* [out] */ int *pcchOut);


void __RPC_STUB ILgCharacterPropertyEngine_GetLineBreakText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_put_LineBreakText_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [size_is][in] */ OLECHAR *prgchIn,
	/* [in] */ int cchMax);


void __RPC_STUB ILgCharacterPropertyEngine_put_LineBreakText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_LineBreakBefore_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ichIn,
	/* [out] */ int *pichOut,
	/* [out] */ LgLineBreak *plbWeight);


void __RPC_STUB ILgCharacterPropertyEngine_LineBreakBefore_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCharacterPropertyEngine_LineBreakAfter_Proxy(
	ILgCharacterPropertyEngine * This,
	/* [in] */ int ichIn,
	/* [out] */ int *pichOut,
	/* [out] */ LgLineBreak *plbWeight);


void __RPC_STUB ILgCharacterPropertyEngine_LineBreakAfter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgCharacterPropertyEngine_INTERFACE_DEFINED__ */


#ifndef __ILgStringConverter_INTERFACE_DEFINED__
#define __ILgStringConverter_INTERFACE_DEFINED__

/* interface ILgStringConverter */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgStringConverter;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0D224002-03C7-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgStringConverter_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgStringConverter_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgStringConverter_ConvertString(This,bstrIn,pbstrOut)	\
	(This)->lpVtbl -> ConvertString(This,bstrIn,pbstrOut)

#define ILgStringConverter_ConvertStringRgch(This,prgch,cch,cchMax,prgchOut,pcchOut)	\
	(This)->lpVtbl -> ConvertStringRgch(This,prgch,cch,cchMax,prgchOut,pcchOut)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgStringConverter_ConvertString_Proxy(
	ILgStringConverter * This,
	/* [in] */ BSTR bstrIn,
	/* [retval][out] */ BSTR *pbstrOut);


void __RPC_STUB ILgStringConverter_ConvertString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgStringConverter_ConvertStringRgch_Proxy(
	ILgStringConverter * This,
	/* [size_is][in] */ const OLECHAR *prgch,
	/* [in] */ int cch,
	/* [in] */ int cchMax,
	/* [size_is][out] */ OLECHAR *prgchOut,
	/* [out] */ int *pcchOut);


void __RPC_STUB ILgStringConverter_ConvertStringRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgStringConverter_INTERFACE_DEFINED__ */


#ifndef __ILgTokenizer_INTERFACE_DEFINED__
#define __ILgTokenizer_INTERFACE_DEFINED__

/* interface ILgTokenizer */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgTokenizer;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0D224003-03C7-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgTokenizer_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgTokenizer_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgTokenizer_GetToken(This,prgchInput,cch,pichMin,pichLim)	\
	(This)->lpVtbl -> GetToken(This,prgchInput,cch,pichMin,pichLim)

#define ILgTokenizer_get_TokenStart(This,bstrInput,ichFirst,pichMin)	\
	(This)->lpVtbl -> get_TokenStart(This,bstrInput,ichFirst,pichMin)

#define ILgTokenizer_get_TokenEnd(This,bstrInput,ichFirst,pichLim)	\
	(This)->lpVtbl -> get_TokenEnd(This,bstrInput,ichFirst,pichLim)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgTokenizer_GetToken_Proxy(
	ILgTokenizer * This,
	/* [size_is][in] */ OLECHAR *prgchInput,
	/* [in] */ int cch,
	/* [out] */ int *pichMin,
	/* [out] */ int *pichLim);


void __RPC_STUB ILgTokenizer_GetToken_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgTokenizer_get_TokenStart_Proxy(
	ILgTokenizer * This,
	/* [in] */ BSTR bstrInput,
	/* [in] */ int ichFirst,
	/* [retval][out] */ int *pichMin);


void __RPC_STUB ILgTokenizer_get_TokenStart_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgTokenizer_get_TokenEnd_Proxy(
	ILgTokenizer * This,
	/* [in] */ BSTR bstrInput,
	/* [in] */ int ichFirst,
	/* [retval][out] */ int *pichLim);


void __RPC_STUB ILgTokenizer_get_TokenEnd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgTokenizer_INTERFACE_DEFINED__ */


#ifndef __ILgSpellCheckFactory_INTERFACE_DEFINED__
#define __ILgSpellCheckFactory_INTERFACE_DEFINED__

/* interface ILgSpellCheckFactory */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgSpellCheckFactory;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FC1C0D01-0483-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgSpellCheckFactory_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgSpellCheckFactory_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgSpellCheckFactory_get_Checker(This,ppspchk)	\
	(This)->lpVtbl -> get_Checker(This,ppspchk)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgSpellCheckFactory_get_Checker_Proxy(
	ILgSpellCheckFactory * This,
	/* [retval][out] */ ILgSpellChecker **ppspchk);


void __RPC_STUB ILgSpellCheckFactory_get_Checker_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgSpellCheckFactory_INTERFACE_DEFINED__ */


#ifndef __ILgSpellChecker_INTERFACE_DEFINED__
#define __ILgSpellChecker_INTERFACE_DEFINED__

/* interface ILgSpellChecker */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgSpellChecker;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0D224006-03C7-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgSpellChecker_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgSpellChecker_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgSpellChecker_Init(This,pszwCustom)	\
	(This)->lpVtbl -> Init(This,pszwCustom)

#define ILgSpellChecker_SetOptions(This,grfsplc)	\
	(This)->lpVtbl -> SetOptions(This,grfsplc)

#define ILgSpellChecker_Check(This,prgchw,cchw,pichMinBad,pichLimBad,pbstrBad,pbstrSuggest,pscrs)	\
	(This)->lpVtbl -> Check(This,prgchw,cchw,pichMinBad,pichLimBad,pbstrBad,pbstrSuggest,pscrs)

#define ILgSpellChecker_Suggest(This,prgchw,cchw,fFirst,pbstrSuggest)	\
	(This)->lpVtbl -> Suggest(This,prgchw,cchw,fFirst,pbstrSuggest)

#define ILgSpellChecker_IgnoreAll(This,pszw)	\
	(This)->lpVtbl -> IgnoreAll(This,pszw)

#define ILgSpellChecker_Change(This,pszwSrc,pszwDst,fAll)	\
	(This)->lpVtbl -> Change(This,pszwSrc,pszwDst,fAll)

#define ILgSpellChecker_AddToUser(This,pszw)	\
	(This)->lpVtbl -> AddToUser(This,pszw)

#define ILgSpellChecker_FlushIgnoreList(This)	\
	(This)->lpVtbl -> FlushIgnoreList(This)

#define ILgSpellChecker_FlushChangeList(This,fAll)	\
	(This)->lpVtbl -> FlushChangeList(This,fAll)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgSpellChecker_Init_Proxy(
	ILgSpellChecker * This,
	/* [in] */ LPCOLESTR pszwCustom);


void __RPC_STUB ILgSpellChecker_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_SetOptions_Proxy(
	ILgSpellChecker * This,
	/* [in] */ int grfsplc);


void __RPC_STUB ILgSpellChecker_SetOptions_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_Check_Proxy(
	ILgSpellChecker * This,
	/* [size_is][in] */ const OLECHAR *prgchw,
	/* [in] */ int cchw,
	/* [out] */ int *pichMinBad,
	/* [out] */ int *pichLimBad,
	/* [out] */ BSTR *pbstrBad,
	/* [out] */ BSTR *pbstrSuggest,
	/* [out] */ int *pscrs);


void __RPC_STUB ILgSpellChecker_Check_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_Suggest_Proxy(
	ILgSpellChecker * This,
	/* [size_is][in] */ const OLECHAR *prgchw,
	/* [in] */ int cchw,
	/* [in] */ ComBool fFirst,
	/* [out] */ BSTR *pbstrSuggest);


void __RPC_STUB ILgSpellChecker_Suggest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_IgnoreAll_Proxy(
	ILgSpellChecker * This,
	/* [in] */ LPCOLESTR pszw);


void __RPC_STUB ILgSpellChecker_IgnoreAll_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_Change_Proxy(
	ILgSpellChecker * This,
	/* [in] */ LPCOLESTR pszwSrc,
	/* [in] */ LPCOLESTR pszwDst,
	ComBool fAll);


void __RPC_STUB ILgSpellChecker_Change_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_AddToUser_Proxy(
	ILgSpellChecker * This,
	/* [in] */ LPCOLESTR pszw);


void __RPC_STUB ILgSpellChecker_AddToUser_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_FlushIgnoreList_Proxy(
	ILgSpellChecker * This);


void __RPC_STUB ILgSpellChecker_FlushIgnoreList_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSpellChecker_FlushChangeList_Proxy(
	ILgSpellChecker * This,
	/* [in] */ ComBool fAll);


void __RPC_STUB ILgSpellChecker_FlushChangeList_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgSpellChecker_INTERFACE_DEFINED__ */


#ifndef __ILgCollatingEngine_INTERFACE_DEFINED__
#define __ILgCollatingEngine_INTERFACE_DEFINED__

/* interface ILgCollatingEngine */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgCollatingEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("DB78D60B-E43E-4464-B8AE-C5C9A00E2C04")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgCollatingEngine_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgCollatingEngine_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgCollatingEngine_get_SortKey(This,bstrValue,colopt,pbstrKey)	\
	(This)->lpVtbl -> get_SortKey(This,bstrValue,colopt,pbstrKey)

#define ILgCollatingEngine_SortKeyRgch(This,pch,cchIn,colopt,cchMaxOut,pchKey,pcchOut)	\
	(This)->lpVtbl -> SortKeyRgch(This,pch,cchIn,colopt,cchMaxOut,pchKey,pcchOut)

#define ILgCollatingEngine_Compare(This,bstrValue1,bstrValue2,colopt,pnVal)	\
	(This)->lpVtbl -> Compare(This,bstrValue1,bstrValue2,colopt,pnVal)

#define ILgCollatingEngine_get_WritingSystemFactory(This,ppwsf)	\
	(This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf)

#define ILgCollatingEngine_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#define ILgCollatingEngine_get_SortKeyVariant(This,bstrValue,colopt,psaKey)	\
	(This)->lpVtbl -> get_SortKeyVariant(This,bstrValue,colopt,psaKey)

#define ILgCollatingEngine_CompareVariant(This,saValue1,saValue2,colopt,pnVal)	\
	(This)->lpVtbl -> CompareVariant(This,saValue1,saValue2,colopt,pnVal)

#define ILgCollatingEngine_Open(This,bstrLocale)	\
	(This)->lpVtbl -> Open(This,bstrLocale)

#define ILgCollatingEngine_Close(This)	\
	(This)->lpVtbl -> Close(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollatingEngine_get_SortKey_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ BSTR bstrValue,
	/* [in] */ LgCollatingOptions colopt,
	/* [retval][out] */ BSTR *pbstrKey);


void __RPC_STUB ILgCollatingEngine_get_SortKey_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgCollatingEngine_SortKeyRgch_Proxy(
	ILgCollatingEngine * This,
	/* [size_is][in] */ const OLECHAR *pch,
	/* [in] */ int cchIn,
	/* [in] */ LgCollatingOptions colopt,
	/* [in] */ int cchMaxOut,
	/* [size_is][out] */ OLECHAR *pchKey,
	/* [out] */ int *pcchOut);


void __RPC_STUB ILgCollatingEngine_SortKeyRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollatingEngine_Compare_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ BSTR bstrValue1,
	/* [in] */ BSTR bstrValue2,
	/* [in] */ LgCollatingOptions colopt,
	/* [retval][out] */ int *pnVal);


void __RPC_STUB ILgCollatingEngine_Compare_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollatingEngine_get_WritingSystemFactory_Proxy(
	ILgCollatingEngine * This,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgCollatingEngine_get_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ILgCollatingEngine_putref_WritingSystemFactory_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB ILgCollatingEngine_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollatingEngine_get_SortKeyVariant_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ BSTR bstrValue,
	/* [in] */ LgCollatingOptions colopt,
	/* [retval][out] */ VARIANT *psaKey);


void __RPC_STUB ILgCollatingEngine_get_SortKeyVariant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollatingEngine_CompareVariant_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ VARIANT saValue1,
	/* [in] */ VARIANT saValue2,
	/* [in] */ LgCollatingOptions colopt,
	/* [retval][out] */ int *pnVal);


void __RPC_STUB ILgCollatingEngine_CompareVariant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollatingEngine_Open_Proxy(
	ILgCollatingEngine * This,
	/* [in] */ BSTR bstrLocale);


void __RPC_STUB ILgCollatingEngine_Open_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollatingEngine_Close_Proxy(
	ILgCollatingEngine * This);


void __RPC_STUB ILgCollatingEngine_Close_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgCollatingEngine_INTERFACE_DEFINED__ */


#ifndef __ILgSearchEngine_INTERFACE_DEFINED__
#define __ILgSearchEngine_INTERFACE_DEFINED__

/* interface ILgSearchEngine */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgSearchEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0D224001-03C7-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgSearchEngine_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgSearchEngine_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgSearchEngine_SetPattern(This,bstrPattern,fIgnoreCase,fIgnoreModifiers,fUseSoundAlike,fUseWildCards)	\
	(This)->lpVtbl -> SetPattern(This,bstrPattern,fIgnoreCase,fIgnoreModifiers,fUseSoundAlike,fUseWildCards)

#define ILgSearchEngine_SetReplacePattern(This,bstrPattern)	\
	(This)->lpVtbl -> SetReplacePattern(This,bstrPattern)

#define ILgSearchEngine_ShowPatternDialog(This,bstrTitle,pwse,fForReplace,pfGoAhead)	\
	(This)->lpVtbl -> ShowPatternDialog(This,bstrTitle,pwse,fForReplace,pfGoAhead)

#define ILgSearchEngine_FindString(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pfFound)	\
	(This)->lpVtbl -> FindString(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pfFound)

#define ILgSearchEngine_FindReplace(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pbstrReplacement)	\
	(This)->lpVtbl -> FindReplace(This,bstrSource,ichFirst,ichMinFound,ichLimFound,pbstrReplacement)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgSearchEngine_SetPattern_Proxy(
	ILgSearchEngine * This,
	/* [in] */ BSTR bstrPattern,
	/* [in] */ ComBool fIgnoreCase,
	/* [in] */ ComBool fIgnoreModifiers,
	/* [in] */ ComBool fUseSoundAlike,
	/* [in] */ ComBool fUseWildCards);


void __RPC_STUB ILgSearchEngine_SetPattern_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSearchEngine_SetReplacePattern_Proxy(
	ILgSearchEngine * This,
	/* [in] */ BSTR bstrPattern);


void __RPC_STUB ILgSearchEngine_SetReplacePattern_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSearchEngine_ShowPatternDialog_Proxy(
	ILgSearchEngine * This,
	/* [in] */ BSTR bstrTitle,
	/* [in] */ ILgWritingSystem *pwse,
	/* [in] */ ComBool fForReplace,
	/* [retval][out] */ ComBool *pfGoAhead);


void __RPC_STUB ILgSearchEngine_ShowPatternDialog_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSearchEngine_FindString_Proxy(
	ILgSearchEngine * This,
	/* [in] */ BSTR bstrSource,
	/* [in] */ int ichFirst,
	/* [out] */ int *ichMinFound,
	/* [out] */ int *ichLimFound,
	/* [retval][out] */ ComBool *pfFound);


void __RPC_STUB ILgSearchEngine_FindString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgSearchEngine_FindReplace_Proxy(
	ILgSearchEngine * This,
	/* [in] */ BSTR bstrSource,
	/* [in] */ int ichFirst,
	/* [out] */ int *ichMinFound,
	/* [out] */ int *ichLimFound,
	/* [retval][out] */ BSTR *pbstrReplacement);


void __RPC_STUB ILgSearchEngine_FindReplace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgSearchEngine_INTERFACE_DEFINED__ */


#ifndef __ILgCollation_INTERFACE_DEFINED__
#define __ILgCollation_INTERFACE_DEFINED__

/* interface ILgCollation */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgCollation;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("254DB9E3-0265-49CF-A19F-3C75E8525A28")
	ILgCollation : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Name(
			/* [in] */ int ws,
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NameWsCount(
			/* [retval][out] */ int *pcws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NameWss(
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Hvo(
			/* [retval][out] */ int *phvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WinLCID(
			/* [retval][out] */ int *pnCode) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_WinLCID(
			/* [in] */ int nCode) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WinCollation(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_WinCollation(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuResourceName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuResourceName(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuResourceText(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuResourceText(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Dirty(
			/* [retval][out] */ ComBool *pf) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Dirty(
			/* [in] */ ComBool fDirty) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteAsXml(
			/* [in] */ IStream *pstrm,
			/* [in] */ int cchIndent) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual HRESULT STDMETHODCALLTYPE Deserialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuRules(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuRules(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory(
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgCollationVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgCollation * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgCollation * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgCollation * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			ILgCollation * This,
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Name )(
			ILgCollation * This,
			/* [in] */ int ws,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NameWsCount )(
			ILgCollation * This,
			/* [retval][out] */ int *pcws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NameWss )(
			ILgCollation * This,
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Hvo )(
			ILgCollation * This,
			/* [retval][out] */ int *phvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WinLCID )(
			ILgCollation * This,
			/* [retval][out] */ int *pnCode);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_WinLCID )(
			ILgCollation * This,
			/* [in] */ int nCode);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WinCollation )(
			ILgCollation * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_WinCollation )(
			ILgCollation * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuResourceName )(
			ILgCollation * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuResourceName )(
			ILgCollation * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuResourceText )(
			ILgCollation * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuResourceText )(
			ILgCollation * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Dirty )(
			ILgCollation * This,
			/* [retval][out] */ ComBool *pf);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Dirty )(
			ILgCollation * This,
			/* [in] */ ComBool fDirty);

		HRESULT ( STDMETHODCALLTYPE *WriteAsXml )(
			ILgCollation * This,
			/* [in] */ IStream *pstrm,
			/* [in] */ int cchIndent);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			ILgCollation * This,
			/* [in] */ IStorage *pstg);

		HRESULT ( STDMETHODCALLTYPE *Deserialize )(
			ILgCollation * This,
			/* [in] */ IStorage *pstg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuRules )(
			ILgCollation * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuRules )(
			ILgCollation * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )(
			ILgCollation * This,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			ILgCollation * This,
			/* [in] */ ILgWritingSystemFactory *pwsf);

		END_INTERFACE
	} ILgCollationVtbl;

	interface ILgCollation
	{
		CONST_VTBL struct ILgCollationVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgCollation_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgCollation_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgCollation_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgCollation_get_Name(This,ws,pbstr)	\
	(This)->lpVtbl -> get_Name(This,ws,pbstr)

#define ILgCollation_put_Name(This,ws,bstr)	\
	(This)->lpVtbl -> put_Name(This,ws,bstr)

#define ILgCollation_get_NameWsCount(This,pcws)	\
	(This)->lpVtbl -> get_NameWsCount(This,pcws)

#define ILgCollation_get_NameWss(This,cws,prgws)	\
	(This)->lpVtbl -> get_NameWss(This,cws,prgws)

#define ILgCollation_get_Hvo(This,phvo)	\
	(This)->lpVtbl -> get_Hvo(This,phvo)

#define ILgCollation_get_WinLCID(This,pnCode)	\
	(This)->lpVtbl -> get_WinLCID(This,pnCode)

#define ILgCollation_put_WinLCID(This,nCode)	\
	(This)->lpVtbl -> put_WinLCID(This,nCode)

#define ILgCollation_get_WinCollation(This,pbstr)	\
	(This)->lpVtbl -> get_WinCollation(This,pbstr)

#define ILgCollation_put_WinCollation(This,bstr)	\
	(This)->lpVtbl -> put_WinCollation(This,bstr)

#define ILgCollation_get_IcuResourceName(This,pbstr)	\
	(This)->lpVtbl -> get_IcuResourceName(This,pbstr)

#define ILgCollation_put_IcuResourceName(This,bstr)	\
	(This)->lpVtbl -> put_IcuResourceName(This,bstr)

#define ILgCollation_get_IcuResourceText(This,pbstr)	\
	(This)->lpVtbl -> get_IcuResourceText(This,pbstr)

#define ILgCollation_put_IcuResourceText(This,bstr)	\
	(This)->lpVtbl -> put_IcuResourceText(This,bstr)

#define ILgCollation_get_Dirty(This,pf)	\
	(This)->lpVtbl -> get_Dirty(This,pf)

#define ILgCollation_put_Dirty(This,fDirty)	\
	(This)->lpVtbl -> put_Dirty(This,fDirty)

#define ILgCollation_WriteAsXml(This,pstrm,cchIndent)	\
	(This)->lpVtbl -> WriteAsXml(This,pstrm,cchIndent)

#define ILgCollation_Serialize(This,pstg)	\
	(This)->lpVtbl -> Serialize(This,pstg)

#define ILgCollation_Deserialize(This,pstg)	\
	(This)->lpVtbl -> Deserialize(This,pstg)

#define ILgCollation_get_IcuRules(This,pbstr)	\
	(This)->lpVtbl -> get_IcuRules(This,pbstr)

#define ILgCollation_put_IcuRules(This,bstr)	\
	(This)->lpVtbl -> put_IcuRules(This,bstr)

#define ILgCollation_get_WritingSystemFactory(This,ppwsf)	\
	(This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf)

#define ILgCollation_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_Name_Proxy(
	ILgCollation * This,
	/* [in] */ int ws,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCollation_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_Name_Proxy(
	ILgCollation * This,
	/* [in] */ int ws,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgCollation_put_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_NameWsCount_Proxy(
	ILgCollation * This,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ILgCollation_get_NameWsCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_NameWss_Proxy(
	ILgCollation * This,
	/* [in] */ int cws,
	/* [size_is][out] */ int *prgws);


void __RPC_STUB ILgCollation_get_NameWss_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_Hvo_Proxy(
	ILgCollation * This,
	/* [retval][out] */ int *phvo);


void __RPC_STUB ILgCollation_get_Hvo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_WinLCID_Proxy(
	ILgCollation * This,
	/* [retval][out] */ int *pnCode);


void __RPC_STUB ILgCollation_get_WinLCID_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_WinLCID_Proxy(
	ILgCollation * This,
	/* [in] */ int nCode);


void __RPC_STUB ILgCollation_put_WinLCID_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_WinCollation_Proxy(
	ILgCollation * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCollation_get_WinCollation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_WinCollation_Proxy(
	ILgCollation * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgCollation_put_WinCollation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_IcuResourceName_Proxy(
	ILgCollation * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCollation_get_IcuResourceName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_IcuResourceName_Proxy(
	ILgCollation * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgCollation_put_IcuResourceName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_IcuResourceText_Proxy(
	ILgCollation * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCollation_get_IcuResourceText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_IcuResourceText_Proxy(
	ILgCollation * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgCollation_put_IcuResourceText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_Dirty_Proxy(
	ILgCollation * This,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB ILgCollation_get_Dirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_Dirty_Proxy(
	ILgCollation * This,
	/* [in] */ ComBool fDirty);


void __RPC_STUB ILgCollation_put_Dirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollation_WriteAsXml_Proxy(
	ILgCollation * This,
	/* [in] */ IStream *pstrm,
	/* [in] */ int cchIndent);


void __RPC_STUB ILgCollation_WriteAsXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollation_Serialize_Proxy(
	ILgCollation * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgCollation_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCollation_Deserialize_Proxy(
	ILgCollation * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgCollation_Deserialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_IcuRules_Proxy(
	ILgCollation * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgCollation_get_IcuRules_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgCollation_put_IcuRules_Proxy(
	ILgCollation * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgCollation_put_IcuRules_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgCollation_get_WritingSystemFactory_Proxy(
	ILgCollation * This,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgCollation_get_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ILgCollation_putref_WritingSystemFactory_Proxy(
	ILgCollation * This,
	/* [in] */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB ILgCollation_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgCollation_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystem_INTERFACE_DEFINED__
#define __ILgWritingSystem_INTERFACE_DEFINED__

/* interface ILgWritingSystem */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgWritingSystem;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("28BC5EDC-3EF3-4db2-8B90-556200FD97ED")
	ILgWritingSystem : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystem(
			/* [retval][out] */ int *pws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NameWsCount(
			/* [retval][out] */ int *pcws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NameWss(
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstrName) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Name(
			/* [in] */ int ws,
			/* [in] */ BSTR bstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Locale(
			/* [retval][out] */ int *pnLocale) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Locale(
			/* [in] */ int nLocale) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConverterFrom(
			/* [in] */ int ws,
			/* [retval][out] */ ILgStringConverter **ppstrconv) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NormalizeEngine(
			/* [retval][out] */ ILgStringConverter **ppstrconv) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WordBreakEngine(
			/* [retval][out] */ ILgTokenizer **pptoker) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SpellingFactory(
			/* [retval][out] */ ILgSpellCheckFactory **ppspfact) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SpellCheckEngine(
			/* [retval][out] */ ILgSpellChecker **ppspchk) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SearchEngine(
			/* [retval][out] */ ILgSearchEngine **ppsrcheng) = 0;

		virtual HRESULT STDMETHODCALLTYPE CompileEngines( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Dirty(
			/* [retval][out] */ ComBool *pf) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Dirty(
			/* [in] */ ComBool fDirty) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory(
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteAsXml(
			/* [in] */ IStream *pstrm,
			/* [in] */ int cchIndent) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual HRESULT STDMETHODCALLTYPE Deserialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RightToLeft(
			/* [retval][out] */ ComBool *pfRightToLeft) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_RightToLeft(
			/* [in] */ ComBool fRightToLeft) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Renderer(
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppreneng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontVariation(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontVariation(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SansFontVariation(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SansFontVariation(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultSerif(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DefaultSerif(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultSansSerif(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DefaultSansSerif(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultMonospace(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DefaultMonospace(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_KeyMan(
			/* [retval][out] */ ComBool *pfKeyMan) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_KeyMan(
			/* [in] */ ComBool fKeyMan) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UiName(
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CollationCount(
			/* [retval][out] */ int *pccoll) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Collation(
			/* [in] */ int icoll,
			/* [retval][out] */ ILgCollation **ppcoll) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Collation(
			/* [in] */ int icoll,
			/* [in] */ ILgCollation *pcoll) = 0;

		virtual HRESULT STDMETHODCALLTYPE RemoveCollation(
			/* [in] */ int icoll) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Abbr(
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Abbr(
			/* [in] */ int ws,
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AbbrWsCount(
			/* [retval][out] */ int *pcws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AbbrWss(
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description(
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Description(
			/* [in] */ int ws,
			/* [in] */ ITsString *ptss) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DescriptionWsCount(
			/* [retval][out] */ int *pcws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DescriptionWss(
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CollatingEngine(
			/* [retval][out] */ ILgCollatingEngine **ppcoleng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CharPropEngine(
			/* [retval][out] */ ILgCharacterPropertyEngine **pppropeng) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTracing(
			/* [in] */ int n) = 0;

		virtual HRESULT STDMETHODCALLTYPE InterpretChrp(
			/* [out][in] */ LgCharRenderProps *pchrp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuLocale(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuLocale(
			/* [in] */ BSTR bstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetIcuLocaleParts(
			/* [out] */ BSTR *pbstrLanguage,
			/* [out] */ BSTR *pbstrCountry,
			/* [out] */ BSTR *pbstrVariant) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LegacyMapping(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_LegacyMapping(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_KeymanKbdName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_KeymanKbdName(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LanguageName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CountryName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VariantName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LanguageAbbr(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CountryAbbr(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VariantAbbr(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE SaveIfDirty(
			/* [in] */ IOleDbEncap *pode) = 0;

		virtual HRESULT STDMETHODCALLTYPE InstallLanguage(
			ComBool fForce) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LastModified(
			/* [retval][out] */ DATE *pdate) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_LastModified(
			/* [in] */ DATE date) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CurrentInputLanguage(
			/* [retval][out] */ int *pnLangId) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_CurrentInputLanguage(
			/* [in] */ int nLangId) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgWritingSystemVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgWritingSystem * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgWritingSystem * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgWritingSystem * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystem )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NameWsCount )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pcws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NameWss )(
			ILgWritingSystem * This,
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstrName);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Name )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [in] */ BSTR bstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pnLocale);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Locale )(
			ILgWritingSystem * This,
			/* [in] */ int nLocale);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ConverterFrom )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [retval][out] */ ILgStringConverter **ppstrconv);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NormalizeEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgStringConverter **ppstrconv);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WordBreakEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgTokenizer **pptoker);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SpellingFactory )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgSpellCheckFactory **ppspfact);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SpellCheckEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgSpellChecker **ppspchk);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SearchEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgSearchEngine **ppsrcheng);

		HRESULT ( STDMETHODCALLTYPE *CompileEngines )(
			ILgWritingSystem * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Dirty )(
			ILgWritingSystem * This,
			/* [retval][out] */ ComBool *pf);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Dirty )(
			ILgWritingSystem * This,
			/* [in] */ ComBool fDirty);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			ILgWritingSystem * This,
			/* [in] */ ILgWritingSystemFactory *pwsf);

		HRESULT ( STDMETHODCALLTYPE *WriteAsXml )(
			ILgWritingSystem * This,
			/* [in] */ IStream *pstrm,
			/* [in] */ int cchIndent);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			ILgWritingSystem * This,
			/* [in] */ IStorage *pstg);

		HRESULT ( STDMETHODCALLTYPE *Deserialize )(
			ILgWritingSystem * This,
			/* [in] */ IStorage *pstg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RightToLeft )(
			ILgWritingSystem * This,
			/* [retval][out] */ ComBool *pfRightToLeft);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_RightToLeft )(
			ILgWritingSystem * This,
			/* [in] */ ComBool fRightToLeft);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Renderer )(
			ILgWritingSystem * This,
			/* [in] */ IVwGraphics *pvg,
			/* [retval][out] */ IRenderEngine **ppreneng);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontVariation )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontVariation )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SansFontVariation )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SansFontVariation )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultSerif )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DefaultSerif )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultSansSerif )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DefaultSansSerif )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultMonospace )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DefaultMonospace )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_KeyMan )(
			ILgWritingSystem * This,
			/* [retval][out] */ ComBool *pfKeyMan);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_KeyMan )(
			ILgWritingSystem * This,
			/* [in] */ ComBool fKeyMan);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UiName )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CollationCount )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pccoll);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Collation )(
			ILgWritingSystem * This,
			/* [in] */ int icoll,
			/* [retval][out] */ ILgCollation **ppcoll);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Collation )(
			ILgWritingSystem * This,
			/* [in] */ int icoll,
			/* [in] */ ILgCollation *pcoll);

		HRESULT ( STDMETHODCALLTYPE *RemoveCollation )(
			ILgWritingSystem * This,
			/* [in] */ int icoll);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Abbr )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Abbr )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AbbrWsCount )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pcws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AbbrWss )(
			ILgWritingSystem * This,
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [retval][out] */ ITsString **pptss);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Description )(
			ILgWritingSystem * This,
			/* [in] */ int ws,
			/* [in] */ ITsString *ptss);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DescriptionWsCount )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pcws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DescriptionWss )(
			ILgWritingSystem * This,
			/* [in] */ int cws,
			/* [size_is][out] */ int *prgws);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CollatingEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgCollatingEngine **ppcoleng);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CharPropEngine )(
			ILgWritingSystem * This,
			/* [retval][out] */ ILgCharacterPropertyEngine **pppropeng);

		HRESULT ( STDMETHODCALLTYPE *SetTracing )(
			ILgWritingSystem * This,
			/* [in] */ int n);

		HRESULT ( STDMETHODCALLTYPE *InterpretChrp )(
			ILgWritingSystem * This,
			/* [out][in] */ LgCharRenderProps *pchrp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuLocale )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuLocale )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		HRESULT ( STDMETHODCALLTYPE *GetIcuLocaleParts )(
			ILgWritingSystem * This,
			/* [out] */ BSTR *pbstrLanguage,
			/* [out] */ BSTR *pbstrCountry,
			/* [out] */ BSTR *pbstrVariant);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LegacyMapping )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LegacyMapping )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_KeymanKbdName )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_KeymanKbdName )(
			ILgWritingSystem * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LanguageName )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CountryName )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VariantName )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LanguageAbbr )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CountryAbbr )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VariantAbbr )(
			ILgWritingSystem * This,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *SaveIfDirty )(
			ILgWritingSystem * This,
			/* [in] */ IOleDbEncap *pode);

		HRESULT ( STDMETHODCALLTYPE *InstallLanguage )(
			ILgWritingSystem * This,
			ComBool fForce);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LastModified )(
			ILgWritingSystem * This,
			/* [retval][out] */ DATE *pdate);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LastModified )(
			ILgWritingSystem * This,
			/* [in] */ DATE date);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CurrentInputLanguage )(
			ILgWritingSystem * This,
			/* [retval][out] */ int *pnLangId);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_CurrentInputLanguage )(
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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgWritingSystem_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgWritingSystem_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgWritingSystem_get_WritingSystem(This,pws)	\
	(This)->lpVtbl -> get_WritingSystem(This,pws)

#define ILgWritingSystem_get_NameWsCount(This,pcws)	\
	(This)->lpVtbl -> get_NameWsCount(This,pcws)

#define ILgWritingSystem_get_NameWss(This,cws,prgws)	\
	(This)->lpVtbl -> get_NameWss(This,cws,prgws)

#define ILgWritingSystem_get_Name(This,ws,pbstrName)	\
	(This)->lpVtbl -> get_Name(This,ws,pbstrName)

#define ILgWritingSystem_put_Name(This,ws,bstrName)	\
	(This)->lpVtbl -> put_Name(This,ws,bstrName)

#define ILgWritingSystem_get_Locale(This,pnLocale)	\
	(This)->lpVtbl -> get_Locale(This,pnLocale)

#define ILgWritingSystem_put_Locale(This,nLocale)	\
	(This)->lpVtbl -> put_Locale(This,nLocale)

#define ILgWritingSystem_get_ConverterFrom(This,ws,ppstrconv)	\
	(This)->lpVtbl -> get_ConverterFrom(This,ws,ppstrconv)

#define ILgWritingSystem_get_NormalizeEngine(This,ppstrconv)	\
	(This)->lpVtbl -> get_NormalizeEngine(This,ppstrconv)

#define ILgWritingSystem_get_WordBreakEngine(This,pptoker)	\
	(This)->lpVtbl -> get_WordBreakEngine(This,pptoker)

#define ILgWritingSystem_get_SpellingFactory(This,ppspfact)	\
	(This)->lpVtbl -> get_SpellingFactory(This,ppspfact)

#define ILgWritingSystem_get_SpellCheckEngine(This,ppspchk)	\
	(This)->lpVtbl -> get_SpellCheckEngine(This,ppspchk)

#define ILgWritingSystem_get_SearchEngine(This,ppsrcheng)	\
	(This)->lpVtbl -> get_SearchEngine(This,ppsrcheng)

#define ILgWritingSystem_CompileEngines(This)	\
	(This)->lpVtbl -> CompileEngines(This)

#define ILgWritingSystem_get_Dirty(This,pf)	\
	(This)->lpVtbl -> get_Dirty(This,pf)

#define ILgWritingSystem_put_Dirty(This,fDirty)	\
	(This)->lpVtbl -> put_Dirty(This,fDirty)

#define ILgWritingSystem_get_WritingSystemFactory(This,ppwsf)	\
	(This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf)

#define ILgWritingSystem_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#define ILgWritingSystem_WriteAsXml(This,pstrm,cchIndent)	\
	(This)->lpVtbl -> WriteAsXml(This,pstrm,cchIndent)

#define ILgWritingSystem_Serialize(This,pstg)	\
	(This)->lpVtbl -> Serialize(This,pstg)

#define ILgWritingSystem_Deserialize(This,pstg)	\
	(This)->lpVtbl -> Deserialize(This,pstg)

#define ILgWritingSystem_get_RightToLeft(This,pfRightToLeft)	\
	(This)->lpVtbl -> get_RightToLeft(This,pfRightToLeft)

#define ILgWritingSystem_put_RightToLeft(This,fRightToLeft)	\
	(This)->lpVtbl -> put_RightToLeft(This,fRightToLeft)

#define ILgWritingSystem_get_Renderer(This,pvg,ppreneng)	\
	(This)->lpVtbl -> get_Renderer(This,pvg,ppreneng)

#define ILgWritingSystem_get_FontVariation(This,pbstr)	\
	(This)->lpVtbl -> get_FontVariation(This,pbstr)

#define ILgWritingSystem_put_FontVariation(This,bstr)	\
	(This)->lpVtbl -> put_FontVariation(This,bstr)

#define ILgWritingSystem_get_SansFontVariation(This,pbstr)	\
	(This)->lpVtbl -> get_SansFontVariation(This,pbstr)

#define ILgWritingSystem_put_SansFontVariation(This,bstr)	\
	(This)->lpVtbl -> put_SansFontVariation(This,bstr)

#define ILgWritingSystem_get_DefaultSerif(This,pbstr)	\
	(This)->lpVtbl -> get_DefaultSerif(This,pbstr)

#define ILgWritingSystem_put_DefaultSerif(This,bstr)	\
	(This)->lpVtbl -> put_DefaultSerif(This,bstr)

#define ILgWritingSystem_get_DefaultSansSerif(This,pbstr)	\
	(This)->lpVtbl -> get_DefaultSansSerif(This,pbstr)

#define ILgWritingSystem_put_DefaultSansSerif(This,bstr)	\
	(This)->lpVtbl -> put_DefaultSansSerif(This,bstr)

#define ILgWritingSystem_get_DefaultMonospace(This,pbstr)	\
	(This)->lpVtbl -> get_DefaultMonospace(This,pbstr)

#define ILgWritingSystem_put_DefaultMonospace(This,bstr)	\
	(This)->lpVtbl -> put_DefaultMonospace(This,bstr)

#define ILgWritingSystem_get_KeyMan(This,pfKeyMan)	\
	(This)->lpVtbl -> get_KeyMan(This,pfKeyMan)

#define ILgWritingSystem_put_KeyMan(This,fKeyMan)	\
	(This)->lpVtbl -> put_KeyMan(This,fKeyMan)

#define ILgWritingSystem_get_UiName(This,ws,pbstr)	\
	(This)->lpVtbl -> get_UiName(This,ws,pbstr)

#define ILgWritingSystem_get_CollationCount(This,pccoll)	\
	(This)->lpVtbl -> get_CollationCount(This,pccoll)

#define ILgWritingSystem_get_Collation(This,icoll,ppcoll)	\
	(This)->lpVtbl -> get_Collation(This,icoll,ppcoll)

#define ILgWritingSystem_putref_Collation(This,icoll,pcoll)	\
	(This)->lpVtbl -> putref_Collation(This,icoll,pcoll)

#define ILgWritingSystem_RemoveCollation(This,icoll)	\
	(This)->lpVtbl -> RemoveCollation(This,icoll)

#define ILgWritingSystem_get_Abbr(This,ws,pbstr)	\
	(This)->lpVtbl -> get_Abbr(This,ws,pbstr)

#define ILgWritingSystem_put_Abbr(This,ws,bstr)	\
	(This)->lpVtbl -> put_Abbr(This,ws,bstr)

#define ILgWritingSystem_get_AbbrWsCount(This,pcws)	\
	(This)->lpVtbl -> get_AbbrWsCount(This,pcws)

#define ILgWritingSystem_get_AbbrWss(This,cws,prgws)	\
	(This)->lpVtbl -> get_AbbrWss(This,cws,prgws)

#define ILgWritingSystem_get_Description(This,ws,pptss)	\
	(This)->lpVtbl -> get_Description(This,ws,pptss)

#define ILgWritingSystem_put_Description(This,ws,ptss)	\
	(This)->lpVtbl -> put_Description(This,ws,ptss)

#define ILgWritingSystem_get_DescriptionWsCount(This,pcws)	\
	(This)->lpVtbl -> get_DescriptionWsCount(This,pcws)

#define ILgWritingSystem_get_DescriptionWss(This,cws,prgws)	\
	(This)->lpVtbl -> get_DescriptionWss(This,cws,prgws)

#define ILgWritingSystem_get_CollatingEngine(This,ppcoleng)	\
	(This)->lpVtbl -> get_CollatingEngine(This,ppcoleng)

#define ILgWritingSystem_get_CharPropEngine(This,pppropeng)	\
	(This)->lpVtbl -> get_CharPropEngine(This,pppropeng)

#define ILgWritingSystem_SetTracing(This,n)	\
	(This)->lpVtbl -> SetTracing(This,n)

#define ILgWritingSystem_InterpretChrp(This,pchrp)	\
	(This)->lpVtbl -> InterpretChrp(This,pchrp)

#define ILgWritingSystem_get_IcuLocale(This,pbstr)	\
	(This)->lpVtbl -> get_IcuLocale(This,pbstr)

#define ILgWritingSystem_put_IcuLocale(This,bstr)	\
	(This)->lpVtbl -> put_IcuLocale(This,bstr)

#define ILgWritingSystem_GetIcuLocaleParts(This,pbstrLanguage,pbstrCountry,pbstrVariant)	\
	(This)->lpVtbl -> GetIcuLocaleParts(This,pbstrLanguage,pbstrCountry,pbstrVariant)

#define ILgWritingSystem_get_LegacyMapping(This,pbstr)	\
	(This)->lpVtbl -> get_LegacyMapping(This,pbstr)

#define ILgWritingSystem_put_LegacyMapping(This,bstr)	\
	(This)->lpVtbl -> put_LegacyMapping(This,bstr)

#define ILgWritingSystem_get_KeymanKbdName(This,pbstr)	\
	(This)->lpVtbl -> get_KeymanKbdName(This,pbstr)

#define ILgWritingSystem_put_KeymanKbdName(This,bstr)	\
	(This)->lpVtbl -> put_KeymanKbdName(This,bstr)

#define ILgWritingSystem_get_LanguageName(This,pbstr)	\
	(This)->lpVtbl -> get_LanguageName(This,pbstr)

#define ILgWritingSystem_get_CountryName(This,pbstr)	\
	(This)->lpVtbl -> get_CountryName(This,pbstr)

#define ILgWritingSystem_get_VariantName(This,pbstr)	\
	(This)->lpVtbl -> get_VariantName(This,pbstr)

#define ILgWritingSystem_get_LanguageAbbr(This,pbstr)	\
	(This)->lpVtbl -> get_LanguageAbbr(This,pbstr)

#define ILgWritingSystem_get_CountryAbbr(This,pbstr)	\
	(This)->lpVtbl -> get_CountryAbbr(This,pbstr)

#define ILgWritingSystem_get_VariantAbbr(This,pbstr)	\
	(This)->lpVtbl -> get_VariantAbbr(This,pbstr)

#define ILgWritingSystem_SaveIfDirty(This,pode)	\
	(This)->lpVtbl -> SaveIfDirty(This,pode)

#define ILgWritingSystem_InstallLanguage(This,fForce)	\
	(This)->lpVtbl -> InstallLanguage(This,fForce)

#define ILgWritingSystem_get_LastModified(This,pdate)	\
	(This)->lpVtbl -> get_LastModified(This,pdate)

#define ILgWritingSystem_put_LastModified(This,date)	\
	(This)->lpVtbl -> put_LastModified(This,date)

#define ILgWritingSystem_get_CurrentInputLanguage(This,pnLangId)	\
	(This)->lpVtbl -> get_CurrentInputLanguage(This,pnLangId)

#define ILgWritingSystem_put_CurrentInputLanguage(This,nLangId)	\
	(This)->lpVtbl -> put_CurrentInputLanguage(This,nLangId)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_WritingSystem_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pws);


void __RPC_STUB ILgWritingSystem_get_WritingSystem_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_NameWsCount_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ILgWritingSystem_get_NameWsCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_NameWss_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int cws,
	/* [size_is][out] */ int *prgws);


void __RPC_STUB ILgWritingSystem_get_NameWss_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Name_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgWritingSystem_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_Name_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [in] */ BSTR bstrName);


void __RPC_STUB ILgWritingSystem_put_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Locale_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pnLocale);


void __RPC_STUB ILgWritingSystem_get_Locale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_Locale_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int nLocale);


void __RPC_STUB ILgWritingSystem_put_Locale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_ConverterFrom_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [retval][out] */ ILgStringConverter **ppstrconv);


void __RPC_STUB ILgWritingSystem_get_ConverterFrom_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_NormalizeEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgStringConverter **ppstrconv);


void __RPC_STUB ILgWritingSystem_get_NormalizeEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_WordBreakEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgTokenizer **pptoker);


void __RPC_STUB ILgWritingSystem_get_WordBreakEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_SpellingFactory_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgSpellCheckFactory **ppspfact);


void __RPC_STUB ILgWritingSystem_get_SpellingFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_SpellCheckEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgSpellChecker **ppspchk);


void __RPC_STUB ILgWritingSystem_get_SpellCheckEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_SearchEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgSearchEngine **ppsrcheng);


void __RPC_STUB ILgWritingSystem_get_SearchEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_CompileEngines_Proxy(
	ILgWritingSystem * This);


void __RPC_STUB ILgWritingSystem_CompileEngines_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Dirty_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB ILgWritingSystem_get_Dirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_Dirty_Proxy(
	ILgWritingSystem * This,
	/* [in] */ ComBool fDirty);


void __RPC_STUB ILgWritingSystem_put_Dirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_WritingSystemFactory_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgWritingSystem_get_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_putref_WritingSystemFactory_Proxy(
	ILgWritingSystem * This,
	/* [in] */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB ILgWritingSystem_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_WriteAsXml_Proxy(
	ILgWritingSystem * This,
	/* [in] */ IStream *pstrm,
	/* [in] */ int cchIndent);


void __RPC_STUB ILgWritingSystem_WriteAsXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_Serialize_Proxy(
	ILgWritingSystem * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgWritingSystem_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_Deserialize_Proxy(
	ILgWritingSystem * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgWritingSystem_Deserialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_RightToLeft_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ComBool *pfRightToLeft);


void __RPC_STUB ILgWritingSystem_get_RightToLeft_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_RightToLeft_Proxy(
	ILgWritingSystem * This,
	/* [in] */ ComBool fRightToLeft);


void __RPC_STUB ILgWritingSystem_put_RightToLeft_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Renderer_Proxy(
	ILgWritingSystem * This,
	/* [in] */ IVwGraphics *pvg,
	/* [retval][out] */ IRenderEngine **ppreneng);


void __RPC_STUB ILgWritingSystem_get_Renderer_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_FontVariation_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_FontVariation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_FontVariation_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_FontVariation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_SansFontVariation_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_SansFontVariation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_SansFontVariation_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_SansFontVariation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_DefaultSerif_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_DefaultSerif_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_DefaultSerif_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_DefaultSerif_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_DefaultSansSerif_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_DefaultSansSerif_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_DefaultSansSerif_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_DefaultSansSerif_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_DefaultMonospace_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_DefaultMonospace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_DefaultMonospace_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_DefaultMonospace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_KeyMan_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ComBool *pfKeyMan);


void __RPC_STUB ILgWritingSystem_get_KeyMan_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_KeyMan_Proxy(
	ILgWritingSystem * This,
	/* [in] */ ComBool fKeyMan);


void __RPC_STUB ILgWritingSystem_put_KeyMan_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_UiName_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_UiName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CollationCount_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pccoll);


void __RPC_STUB ILgWritingSystem_get_CollationCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Collation_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int icoll,
	/* [retval][out] */ ILgCollation **ppcoll);


void __RPC_STUB ILgWritingSystem_get_Collation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_putref_Collation_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int icoll,
	/* [in] */ ILgCollation *pcoll);


void __RPC_STUB ILgWritingSystem_putref_Collation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_RemoveCollation_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int icoll);


void __RPC_STUB ILgWritingSystem_RemoveCollation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Abbr_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_Abbr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_Abbr_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_Abbr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_AbbrWsCount_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ILgWritingSystem_get_AbbrWsCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_AbbrWss_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int cws,
	/* [size_is][out] */ int *prgws);


void __RPC_STUB ILgWritingSystem_get_AbbrWss_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_Description_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ILgWritingSystem_get_Description_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_Description_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int ws,
	/* [in] */ ITsString *ptss);


void __RPC_STUB ILgWritingSystem_put_Description_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_DescriptionWsCount_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ILgWritingSystem_get_DescriptionWsCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_DescriptionWss_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int cws,
	/* [size_is][out] */ int *prgws);


void __RPC_STUB ILgWritingSystem_get_DescriptionWss_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CollatingEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgCollatingEngine **ppcoleng);


void __RPC_STUB ILgWritingSystem_get_CollatingEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CharPropEngine_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ ILgCharacterPropertyEngine **pppropeng);


void __RPC_STUB ILgWritingSystem_get_CharPropEngine_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_SetTracing_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int n);


void __RPC_STUB ILgWritingSystem_SetTracing_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_InterpretChrp_Proxy(
	ILgWritingSystem * This,
	/* [out][in] */ LgCharRenderProps *pchrp);


void __RPC_STUB ILgWritingSystem_InterpretChrp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_IcuLocale_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_IcuLocale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_IcuLocale_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_IcuLocale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_GetIcuLocaleParts_Proxy(
	ILgWritingSystem * This,
	/* [out] */ BSTR *pbstrLanguage,
	/* [out] */ BSTR *pbstrCountry,
	/* [out] */ BSTR *pbstrVariant);


void __RPC_STUB ILgWritingSystem_GetIcuLocaleParts_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_LegacyMapping_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_LegacyMapping_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_LegacyMapping_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_LegacyMapping_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_KeymanKbdName_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_KeymanKbdName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_KeymanKbdName_Proxy(
	ILgWritingSystem * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB ILgWritingSystem_put_KeymanKbdName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_LanguageName_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_LanguageName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CountryName_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_CountryName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_VariantName_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_VariantName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_LanguageAbbr_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_LanguageAbbr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CountryAbbr_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_CountryAbbr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_VariantAbbr_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgWritingSystem_get_VariantAbbr_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_SaveIfDirty_Proxy(
	ILgWritingSystem * This,
	/* [in] */ IOleDbEncap *pode);


void __RPC_STUB ILgWritingSystem_SaveIfDirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystem_InstallLanguage_Proxy(
	ILgWritingSystem * This,
	ComBool fForce);


void __RPC_STUB ILgWritingSystem_InstallLanguage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_LastModified_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ DATE *pdate);


void __RPC_STUB ILgWritingSystem_get_LastModified_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_LastModified_Proxy(
	ILgWritingSystem * This,
	/* [in] */ DATE date);


void __RPC_STUB ILgWritingSystem_put_LastModified_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_get_CurrentInputLanguage_Proxy(
	ILgWritingSystem * This,
	/* [retval][out] */ int *pnLangId);


void __RPC_STUB ILgWritingSystem_get_CurrentInputLanguage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgWritingSystem_put_CurrentInputLanguage_Proxy(
	ILgWritingSystem * This,
	/* [in] */ int nLangId);


void __RPC_STUB ILgWritingSystem_put_CurrentInputLanguage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgWritingSystem_INTERFACE_DEFINED__ */


#ifndef __ILgTsStringPlusWss_INTERFACE_DEFINED__
#define __ILgTsStringPlusWss_INTERFACE_DEFINED__

/* interface ILgTsStringPlusWss */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgTsStringPlusWss;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("71C8D1ED-49B0-40ef-8423-92B0A5F04B89")
	ILgTsStringPlusWss : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_String(
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [retval][out] */ ITsString **pptss) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_String(
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ ITsString *ptss) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Text(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStorage *pstg) = 0;

		virtual HRESULT STDMETHODCALLTYPE Deserialize(
			/* [in] */ IStorage *pstg) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgTsStringPlusWssVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgTsStringPlusWss * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgTsStringPlusWss * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgTsStringPlusWss * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_String )(
			ILgTsStringPlusWss * This,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [retval][out] */ ITsString **pptss);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_String )(
			ILgTsStringPlusWss * This,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ ITsString *ptss);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Text )(
			ILgTsStringPlusWss * This,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			ILgTsStringPlusWss * This,
			/* [in] */ IStorage *pstg);

		HRESULT ( STDMETHODCALLTYPE *Deserialize )(
			ILgTsStringPlusWss * This,
			/* [in] */ IStorage *pstg);

		END_INTERFACE
	} ILgTsStringPlusWssVtbl;

	interface ILgTsStringPlusWss
	{
		CONST_VTBL struct ILgTsStringPlusWssVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgTsStringPlusWss_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgTsStringPlusWss_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgTsStringPlusWss_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgTsStringPlusWss_get_String(This,pwsf,pptss)	\
	(This)->lpVtbl -> get_String(This,pwsf,pptss)

#define ILgTsStringPlusWss_putref_String(This,pwsf,ptss)	\
	(This)->lpVtbl -> putref_String(This,pwsf,ptss)

#define ILgTsStringPlusWss_get_Text(This,pbstr)	\
	(This)->lpVtbl -> get_Text(This,pbstr)

#define ILgTsStringPlusWss_Serialize(This,pstg)	\
	(This)->lpVtbl -> Serialize(This,pstg)

#define ILgTsStringPlusWss_Deserialize(This,pstg)	\
	(This)->lpVtbl -> Deserialize(This,pstg)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgTsStringPlusWss_get_String_Proxy(
	ILgTsStringPlusWss * This,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [retval][out] */ ITsString **pptss);


void __RPC_STUB ILgTsStringPlusWss_get_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ILgTsStringPlusWss_putref_String_Proxy(
	ILgTsStringPlusWss * This,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [in] */ ITsString *ptss);


void __RPC_STUB ILgTsStringPlusWss_putref_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgTsStringPlusWss_get_Text_Proxy(
	ILgTsStringPlusWss * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgTsStringPlusWss_get_Text_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgTsStringPlusWss_Serialize_Proxy(
	ILgTsStringPlusWss * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgTsStringPlusWss_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgTsStringPlusWss_Deserialize_Proxy(
	ILgTsStringPlusWss * This,
	/* [in] */ IStorage *pstg);


void __RPC_STUB ILgTsStringPlusWss_Deserialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgTsStringPlusWss_INTERFACE_DEFINED__ */


#ifndef __ILgTsDataObject_INTERFACE_DEFINED__
#define __ILgTsDataObject_INTERFACE_DEFINED__

/* interface ILgTsDataObject */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgTsDataObject;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("56CD4356-C349-4927-9E3D-CC0CF0EFF04E")
	ILgTsDataObject : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ ILgTsStringPlusWss *ptsswss) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClipboardType(
			/* [out] */ UINT *type) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgTsDataObjectVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgTsDataObject * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgTsDataObject * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgTsDataObject * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgTsDataObject * This,
			/* [in] */ ILgTsStringPlusWss *ptsswss);

		HRESULT ( STDMETHODCALLTYPE *GetClipboardType )(
			ILgTsDataObject * This,
			/* [out] */ UINT *type);

		END_INTERFACE
	} ILgTsDataObjectVtbl;

	interface ILgTsDataObject
	{
		CONST_VTBL struct ILgTsDataObjectVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgTsDataObject_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgTsDataObject_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgTsDataObject_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgTsDataObject_Init(This,ptsswss)	\
	(This)->lpVtbl -> Init(This,ptsswss)

#define ILgTsDataObject_GetClipboardType(This,type)	\
	(This)->lpVtbl -> GetClipboardType(This,type)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgTsDataObject_Init_Proxy(
	ILgTsDataObject * This,
	/* [in] */ ILgTsStringPlusWss *ptsswss);


void __RPC_STUB ILgTsDataObject_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgTsDataObject_GetClipboardType_Proxy(
	ILgTsDataObject * This,
	/* [out] */ UINT *type);


void __RPC_STUB ILgTsDataObject_GetClipboardType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgTsDataObject_INTERFACE_DEFINED__ */


#ifndef __ILgTextServices_INTERFACE_DEFINED__
#define __ILgTextServices_INTERFACE_DEFINED__

/* interface ILgTextServices */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgTextServices;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("03D86B2C-9FB3-4E33-9B23-6C8BFC18FB1E")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgTextServices_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgTextServices_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgTextServices_SetKeyboard(This,nLcid,bstrKeymanKbd,pnActiveLangId,pbstrActiveKeymanKbd,pfSelectLangPending)	\
	(This)->lpVtbl -> SetKeyboard(This,nLcid,bstrKeymanKbd,pnActiveLangId,pbstrActiveKeymanKbd,pfSelectLangPending)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgTextServices_SetKeyboard_Proxy(
	ILgTextServices * This,
	/* [in] */ int nLcid,
	/* [in] */ BSTR bstrKeymanKbd,
	/* [out][in] */ int *pnActiveLangId,
	/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
	/* [out][in] */ ComBool *pfSelectLangPending);


void __RPC_STUB ILgTextServices_SetKeyboard_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgTextServices_INTERFACE_DEFINED__ */


#ifndef __ILgFontManager_INTERFACE_DEFINED__
#define __ILgFontManager_INTERFACE_DEFINED__

/* interface ILgFontManager */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgFontManager;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("10894680-F384-11d3-B5D1-00400543A266")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgFontManager_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgFontManager_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgFontManager_IsFontAvailable(This,bstrName,pfAvail)	\
	(This)->lpVtbl -> IsFontAvailable(This,bstrName,pfAvail)

#define ILgFontManager_IsFontAvailableRgch(This,cch,prgchName,pfAvail)	\
	(This)->lpVtbl -> IsFontAvailableRgch(This,cch,prgchName,pfAvail)

#define ILgFontManager_AvailableFonts(This,pbstrNames)	\
	(This)->lpVtbl -> AvailableFonts(This,pbstrNames)

#define ILgFontManager_RefreshFontList(This)	\
	(This)->lpVtbl -> RefreshFontList(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgFontManager_IsFontAvailable_Proxy(
	ILgFontManager * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ ComBool *pfAvail);


void __RPC_STUB ILgFontManager_IsFontAvailable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgFontManager_IsFontAvailableRgch_Proxy(
	ILgFontManager * This,
	/* [in] */ int cch,
	/* [in] */ OLECHAR *prgchName,
	/* [retval][out] */ ComBool *pfAvail);


void __RPC_STUB ILgFontManager_IsFontAvailableRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgFontManager_AvailableFonts_Proxy(
	ILgFontManager * This,
	/* [out] */ BSTR *pbstrNames);


void __RPC_STUB ILgFontManager_AvailableFonts_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgFontManager_RefreshFontList_Proxy(
	ILgFontManager * This);


void __RPC_STUB ILgFontManager_RefreshFontList_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgFontManager_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_LgInputMethodEditor;

#ifdef __cplusplus

class DECLSPEC_UUID("F74151C0-ABC0-11d3-BC29-00A0CC3A40C6")
LgInputMethodEditor;
#endif

EXTERN_C const CLSID CLSID_LgFontManager;

#ifdef __cplusplus

class DECLSPEC_UUID("70553ED0-F437-11d3-B5D1-00400543A266")
LgFontManager;
#endif

#ifndef __ILgNumericEngine_INTERFACE_DEFINED__
#define __ILgNumericEngine_INTERFACE_DEFINED__

/* interface ILgNumericEngine */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgNumericEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FC1C0D04-0483-11d3-8078-0000C0FB81B5")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgNumericEngine_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgNumericEngine_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgNumericEngine_get_IntToString(This,n,bstr)	\
	(This)->lpVtbl -> get_IntToString(This,n,bstr)

#define ILgNumericEngine_get_IntToPrettyString(This,n,bstr)	\
	(This)->lpVtbl -> get_IntToPrettyString(This,n,bstr)

#define ILgNumericEngine_get_StringToInt(This,bstr,pn)	\
	(This)->lpVtbl -> get_StringToInt(This,bstr,pn)

#define ILgNumericEngine_StringToIntRgch(This,prgch,cch,pn,pichUnused)	\
	(This)->lpVtbl -> StringToIntRgch(This,prgch,cch,pn,pichUnused)

#define ILgNumericEngine_get_DblToString(This,dbl,cchFracDigits,bstr)	\
	(This)->lpVtbl -> get_DblToString(This,dbl,cchFracDigits,bstr)

#define ILgNumericEngine_get_DblToPrettyString(This,dbl,cchFracDigits,bstr)	\
	(This)->lpVtbl -> get_DblToPrettyString(This,dbl,cchFracDigits,bstr)

#define ILgNumericEngine_get_DblToExpString(This,dbl,cchFracDigits,bstr)	\
	(This)->lpVtbl -> get_DblToExpString(This,dbl,cchFracDigits,bstr)

#define ILgNumericEngine_get_StringToDbl(This,bstr,pdbl)	\
	(This)->lpVtbl -> get_StringToDbl(This,bstr,pdbl)

#define ILgNumericEngine_StringToDblRgch(This,prgch,cch,pdbl,pichUnused)	\
	(This)->lpVtbl -> StringToDblRgch(This,prgch,cch,pdbl,pichUnused)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_IntToString_Proxy(
	ILgNumericEngine * This,
	/* [in] */ int n,
	/* [retval][out] */ BSTR *bstr);


void __RPC_STUB ILgNumericEngine_get_IntToString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_IntToPrettyString_Proxy(
	ILgNumericEngine * This,
	/* [in] */ int n,
	/* [retval][out] */ BSTR *bstr);


void __RPC_STUB ILgNumericEngine_get_IntToPrettyString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_StringToInt_Proxy(
	ILgNumericEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ int *pn);


void __RPC_STUB ILgNumericEngine_get_StringToInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_StringToIntRgch_Proxy(
	ILgNumericEngine * This,
	/* [size_is][in] */ OLECHAR *prgch,
	/* [in] */ int cch,
	/* [out] */ int *pn,
	/* [out] */ int *pichUnused);


void __RPC_STUB ILgNumericEngine_StringToIntRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_DblToString_Proxy(
	ILgNumericEngine * This,
	/* [in] */ double dbl,
	/* [in] */ int cchFracDigits,
	/* [retval][out] */ BSTR *bstr);


void __RPC_STUB ILgNumericEngine_get_DblToString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_DblToPrettyString_Proxy(
	ILgNumericEngine * This,
	/* [in] */ double dbl,
	/* [in] */ int cchFracDigits,
	/* [retval][out] */ BSTR *bstr);


void __RPC_STUB ILgNumericEngine_get_DblToPrettyString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_DblToExpString_Proxy(
	ILgNumericEngine * This,
	/* [in] */ double dbl,
	/* [in] */ int cchFracDigits,
	/* [retval][out] */ BSTR *bstr);


void __RPC_STUB ILgNumericEngine_get_DblToExpString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_get_StringToDbl_Proxy(
	ILgNumericEngine * This,
	/* [in] */ BSTR bstr,
	/* [retval][out] */ double *pdbl);


void __RPC_STUB ILgNumericEngine_get_StringToDbl_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [restricted] */ HRESULT STDMETHODCALLTYPE ILgNumericEngine_StringToDblRgch_Proxy(
	ILgNumericEngine * This,
	/* [size_is][in] */ OLECHAR *prgch,
	/* [in] */ int cch,
	/* [out] */ double *pdbl,
	/* [out] */ int *pichUnused);


void __RPC_STUB ILgNumericEngine_StringToDblRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgNumericEngine_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystemFactoryBuilder_INTERFACE_DEFINED__
#define __ILgWritingSystemFactoryBuilder_INTERFACE_DEFINED__

/* interface ILgWritingSystemFactoryBuilder */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgWritingSystemFactoryBuilder;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8AD52AF0-13A8-4d28-A1EE-71924B36989F")
	ILgWritingSystemFactoryBuilder : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE GetWritingSystemFactory(
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ IStream *pfistLog,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetWritingSystemFactoryNew(
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual HRESULT STDMETHODCALLTYPE Deserialize(
			/* [in] */ IStorage *pstg,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShutdownAllFactories( void) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgWritingSystemFactoryBuilderVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgWritingSystemFactoryBuilder * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgWritingSystemFactoryBuilder * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgWritingSystemFactoryBuilder * This);

		HRESULT ( STDMETHODCALLTYPE *GetWritingSystemFactory )(
			ILgWritingSystemFactoryBuilder * This,
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ IStream *pfistLog,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		HRESULT ( STDMETHODCALLTYPE *GetWritingSystemFactoryNew )(
			ILgWritingSystemFactoryBuilder * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		HRESULT ( STDMETHODCALLTYPE *Deserialize )(
			ILgWritingSystemFactoryBuilder * This,
			/* [in] */ IStorage *pstg,
			/* [retval][out] */ ILgWritingSystemFactory **ppwsf);

		HRESULT ( STDMETHODCALLTYPE *ShutdownAllFactories )(
			ILgWritingSystemFactoryBuilder * This);

		END_INTERFACE
	} ILgWritingSystemFactoryBuilderVtbl;

	interface ILgWritingSystemFactoryBuilder
	{
		CONST_VTBL struct ILgWritingSystemFactoryBuilderVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgWritingSystemFactoryBuilder_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgWritingSystemFactoryBuilder_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgWritingSystemFactoryBuilder_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgWritingSystemFactoryBuilder_GetWritingSystemFactory(This,pode,pfistLog,ppwsf)	\
	(This)->lpVtbl -> GetWritingSystemFactory(This,pode,pfistLog,ppwsf)

#define ILgWritingSystemFactoryBuilder_GetWritingSystemFactoryNew(This,bstrServer,bstrDatabase,pfistLog,ppwsf)	\
	(This)->lpVtbl -> GetWritingSystemFactoryNew(This,bstrServer,bstrDatabase,pfistLog,ppwsf)

#define ILgWritingSystemFactoryBuilder_Deserialize(This,pstg,ppwsf)	\
	(This)->lpVtbl -> Deserialize(This,pstg,ppwsf)

#define ILgWritingSystemFactoryBuilder_ShutdownAllFactories(This)	\
	(This)->lpVtbl -> ShutdownAllFactories(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgWritingSystemFactoryBuilder_GetWritingSystemFactory_Proxy(
	ILgWritingSystemFactoryBuilder * This,
	/* [in] */ IOleDbEncap *pode,
	/* [in] */ IStream *pfistLog,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgWritingSystemFactoryBuilder_GetWritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactoryBuilder_GetWritingSystemFactoryNew_Proxy(
	ILgWritingSystemFactoryBuilder * This,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ IStream *pfistLog,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgWritingSystemFactoryBuilder_GetWritingSystemFactoryNew_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactoryBuilder_Deserialize_Proxy(
	ILgWritingSystemFactoryBuilder * This,
	/* [in] */ IStorage *pstg,
	/* [retval][out] */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ILgWritingSystemFactoryBuilder_Deserialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgWritingSystemFactoryBuilder_ShutdownAllFactories_Proxy(
	ILgWritingSystemFactoryBuilder * This);


void __RPC_STUB ILgWritingSystemFactoryBuilder_ShutdownAllFactories_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgWritingSystemFactoryBuilder_INTERFACE_DEFINED__ */


#ifndef __ILgKeymanHandler_INTERFACE_DEFINED__
#define __ILgKeymanHandler_INTERFACE_DEFINED__

/* interface ILgKeymanHandler */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgKeymanHandler;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("D43F4C58-5E24-4b54-8E4D-F0233B823678")
	ILgKeymanHandler : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ ComBool fForce) = 0;

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
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILgKeymanHandler * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILgKeymanHandler * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ILgKeymanHandler * This,
			/* [in] */ ComBool fForce);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgKeymanHandler_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgKeymanHandler_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgKeymanHandler_Init(This,fForce)	\
	(This)->lpVtbl -> Init(This,fForce)

#define ILgKeymanHandler_get_NLayout(This,pclayout)	\
	(This)->lpVtbl -> get_NLayout(This,pclayout)

#define ILgKeymanHandler_get_Name(This,ilayout,pbstrName)	\
	(This)->lpVtbl -> get_Name(This,ilayout,pbstrName)

#define ILgKeymanHandler_get_ActiveKeyboardName(This,pbstrName)	\
	(This)->lpVtbl -> get_ActiveKeyboardName(This,pbstrName)

#define ILgKeymanHandler_put_ActiveKeyboardName(This,bstrName)	\
	(This)->lpVtbl -> put_ActiveKeyboardName(This,bstrName)

#define ILgKeymanHandler_get_KeymanWindowsMessage(This,pwm)	\
	(This)->lpVtbl -> get_KeymanWindowsMessage(This,pwm)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgKeymanHandler_Init_Proxy(
	ILgKeymanHandler * This,
	/* [in] */ ComBool fForce);


void __RPC_STUB ILgKeymanHandler_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgKeymanHandler_get_NLayout_Proxy(
	ILgKeymanHandler * This,
	/* [retval][out] */ int *pclayout);


void __RPC_STUB ILgKeymanHandler_get_NLayout_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgKeymanHandler_get_Name_Proxy(
	ILgKeymanHandler * This,
	/* [in] */ int ilayout,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgKeymanHandler_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgKeymanHandler_get_ActiveKeyboardName_Proxy(
	ILgKeymanHandler * This,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgKeymanHandler_get_ActiveKeyboardName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ILgKeymanHandler_put_ActiveKeyboardName_Proxy(
	ILgKeymanHandler * This,
	/* [in] */ BSTR bstrName);


void __RPC_STUB ILgKeymanHandler_put_ActiveKeyboardName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgKeymanHandler_get_KeymanWindowsMessage_Proxy(
	ILgKeymanHandler * This,
	/* [retval][out] */ int *pwm);


void __RPC_STUB ILgKeymanHandler_get_KeymanWindowsMessage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgKeymanHandler_INTERFACE_DEFINED__ */


#ifndef __ILgCodePageEnumerator_INTERFACE_DEFINED__
#define __ILgCodePageEnumerator_INTERFACE_DEFINED__

/* interface ILgCodePageEnumerator */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgCodePageEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("62811E4D-5572-4f76-B71F-9F17238338E1")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgCodePageEnumerator_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgCodePageEnumerator_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgCodePageEnumerator_Init(This)	\
	(This)->lpVtbl -> Init(This)

#define ILgCodePageEnumerator_Next(This,pnId,pbstrName)	\
	(This)->lpVtbl -> Next(This,pnId,pbstrName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgCodePageEnumerator_Init_Proxy(
	ILgCodePageEnumerator * This);


void __RPC_STUB ILgCodePageEnumerator_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgCodePageEnumerator_Next_Proxy(
	ILgCodePageEnumerator * This,
	/* [out] */ int *pnId,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgCodePageEnumerator_Next_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgCodePageEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgLanguageEnumerator_INTERFACE_DEFINED__
#define __ILgLanguageEnumerator_INTERFACE_DEFINED__

/* interface ILgLanguageEnumerator */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgLanguageEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("76470164-E990-411d-AF66-42A7192E4C49")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgLanguageEnumerator_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgLanguageEnumerator_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgLanguageEnumerator_Init(This)	\
	(This)->lpVtbl -> Init(This)

#define ILgLanguageEnumerator_Next(This,pnId,pbstrName)	\
	(This)->lpVtbl -> Next(This,pnId,pbstrName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgLanguageEnumerator_Init_Proxy(
	ILgLanguageEnumerator * This);


void __RPC_STUB ILgLanguageEnumerator_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ILgLanguageEnumerator_Next_Proxy(
	ILgLanguageEnumerator * This,
	/* [out] */ int *pnId,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgLanguageEnumerator_Next_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgLanguageEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuConverterEnumerator_INTERFACE_DEFINED__
#define __ILgIcuConverterEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuConverterEnumerator */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgIcuConverterEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("34D4E39C-C3B6-413e-9A4E-4457BBB02FE8")
	ILgIcuConverterEnumerator : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count(
			/* [retval][out] */ int *pcconv) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConverterName(
			/* [in] */ int iconv,
			/* [out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConverterId(
			/* [in] */ int iconv,
			/* [out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuConverterEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuConverterEnumerator * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ConverterId )(
			ILgIcuConverterEnumerator * This,
			/* [in] */ int iconv,
			/* [out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgIcuConverterEnumeratorVtbl;

	interface ILgIcuConverterEnumerator
	{
		CONST_VTBL struct ILgIcuConverterEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuConverterEnumerator_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgIcuConverterEnumerator_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgIcuConverterEnumerator_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgIcuConverterEnumerator_get_Count(This,pcconv)	\
	(This)->lpVtbl -> get_Count(This,pcconv)

#define ILgIcuConverterEnumerator_get_ConverterName(This,iconv,pbstrName)	\
	(This)->lpVtbl -> get_ConverterName(This,iconv,pbstrName)

#define ILgIcuConverterEnumerator_get_ConverterId(This,iconv,pbstrName)	\
	(This)->lpVtbl -> get_ConverterId(This,iconv,pbstrName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuConverterEnumerator_get_Count_Proxy(
	ILgIcuConverterEnumerator * This,
	/* [retval][out] */ int *pcconv);


void __RPC_STUB ILgIcuConverterEnumerator_get_Count_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuConverterEnumerator_get_ConverterName_Proxy(
	ILgIcuConverterEnumerator * This,
	/* [in] */ int iconv,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuConverterEnumerator_get_ConverterName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuConverterEnumerator_get_ConverterId_Proxy(
	ILgIcuConverterEnumerator * This,
	/* [in] */ int iconv,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuConverterEnumerator_get_ConverterId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgIcuConverterEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__
#define __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuTransliteratorEnumerator */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgIcuTransliteratorEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("B26A6461-582C-4873-B3F5-673104D1AC37")
	ILgIcuTransliteratorEnumerator : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count(
			/* [retval][out] */ int *pctrans) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TransliteratorName(
			/* [in] */ int itrans,
			/* [out] */ BSTR *pbstrName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TransliteratorId(
			/* [in] */ int iconv,
			/* [out] */ BSTR *pbstrName) = 0;

	};

#else 	/* C style interface */

	typedef struct ILgIcuTransliteratorEnumeratorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILgIcuTransliteratorEnumerator * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

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
			/* [out] */ BSTR *pbstrName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TransliteratorId )(
			ILgIcuTransliteratorEnumerator * This,
			/* [in] */ int iconv,
			/* [out] */ BSTR *pbstrName);

		END_INTERFACE
	} ILgIcuTransliteratorEnumeratorVtbl;

	interface ILgIcuTransliteratorEnumerator
	{
		CONST_VTBL struct ILgIcuTransliteratorEnumeratorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILgIcuTransliteratorEnumerator_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgIcuTransliteratorEnumerator_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgIcuTransliteratorEnumerator_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgIcuTransliteratorEnumerator_get_Count(This,pctrans)	\
	(This)->lpVtbl -> get_Count(This,pctrans)

#define ILgIcuTransliteratorEnumerator_get_TransliteratorName(This,itrans,pbstrName)	\
	(This)->lpVtbl -> get_TransliteratorName(This,itrans,pbstrName)

#define ILgIcuTransliteratorEnumerator_get_TransliteratorId(This,iconv,pbstrName)	\
	(This)->lpVtbl -> get_TransliteratorId(This,iconv,pbstrName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuTransliteratorEnumerator_get_Count_Proxy(
	ILgIcuTransliteratorEnumerator * This,
	/* [retval][out] */ int *pctrans);


void __RPC_STUB ILgIcuTransliteratorEnumerator_get_Count_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuTransliteratorEnumerator_get_TransliteratorName_Proxy(
	ILgIcuTransliteratorEnumerator * This,
	/* [in] */ int itrans,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuTransliteratorEnumerator_get_TransliteratorName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuTransliteratorEnumerator_get_TransliteratorId_Proxy(
	ILgIcuTransliteratorEnumerator * This,
	/* [in] */ int iconv,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuTransliteratorEnumerator_get_TransliteratorId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgIcuTransliteratorEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__
#define __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__

/* interface ILgIcuLocaleEnumerator */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgIcuLocaleEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("00C88119-F57D-4e7b-A03B-EDB0BC3B57EE")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgIcuLocaleEnumerator_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgIcuLocaleEnumerator_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgIcuLocaleEnumerator_get_Count(This,pclocale)	\
	(This)->lpVtbl -> get_Count(This,pclocale)

#define ILgIcuLocaleEnumerator_get_Name(This,iloc,pbstrName)	\
	(This)->lpVtbl -> get_Name(This,iloc,pbstrName)

#define ILgIcuLocaleEnumerator_get_Language(This,iloc,pbstrName)	\
	(This)->lpVtbl -> get_Language(This,iloc,pbstrName)

#define ILgIcuLocaleEnumerator_get_Country(This,iloc,pbstrName)	\
	(This)->lpVtbl -> get_Country(This,iloc,pbstrName)

#define ILgIcuLocaleEnumerator_get_Variant(This,iloc,pbstrName)	\
	(This)->lpVtbl -> get_Variant(This,iloc,pbstrName)

#define ILgIcuLocaleEnumerator_get_DisplayName(This,iloc,bstrLocaleName,pbstrName)	\
	(This)->lpVtbl -> get_DisplayName(This,iloc,bstrLocaleName,pbstrName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_Count_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [retval][out] */ int *pclocale);


void __RPC_STUB ILgIcuLocaleEnumerator_get_Count_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_Name_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [in] */ int iloc,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuLocaleEnumerator_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_Language_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [in] */ int iloc,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuLocaleEnumerator_get_Language_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_Country_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [in] */ int iloc,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuLocaleEnumerator_get_Country_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_Variant_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [in] */ int iloc,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuLocaleEnumerator_get_Variant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuLocaleEnumerator_get_DisplayName_Proxy(
	ILgIcuLocaleEnumerator * This,
	/* [in] */ int iloc,
	/* [in] */ BSTR bstrLocaleName,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuLocaleEnumerator_get_DisplayName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgIcuLocaleEnumerator_INTERFACE_DEFINED__ */


#ifndef __ILgIcuResourceBundle_INTERFACE_DEFINED__
#define __ILgIcuResourceBundle_INTERFACE_DEFINED__

/* interface ILgIcuResourceBundle */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ILgIcuResourceBundle;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("4518189C-E545-48b4-8653-D829D1ECB778")
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
			/* [iid_is][out] */ void **ppvObject);

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
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ILgIcuResourceBundle_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ILgIcuResourceBundle_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ILgIcuResourceBundle_Init(This,bstrPath,locale)	\
	(This)->lpVtbl -> Init(This,bstrPath,locale)

#define ILgIcuResourceBundle_get_Key(This,pbstrKey)	\
	(This)->lpVtbl -> get_Key(This,pbstrKey)

#define ILgIcuResourceBundle_get_String(This,pbstrString)	\
	(This)->lpVtbl -> get_String(This,pbstrString)

#define ILgIcuResourceBundle_get_Name(This,pbstrName)	\
	(This)->lpVtbl -> get_Name(This,pbstrName)

#define ILgIcuResourceBundle_get_GetSubsection(This,bstrSectionName,pprb)	\
	(This)->lpVtbl -> get_GetSubsection(This,bstrSectionName,pprb)

#define ILgIcuResourceBundle_get_HasNext(This,pfHasNext)	\
	(This)->lpVtbl -> get_HasNext(This,pfHasNext)

#define ILgIcuResourceBundle_get_Next(This,pprb)	\
	(This)->lpVtbl -> get_Next(This,pprb)

#define ILgIcuResourceBundle_get_Size(This,pcrb)	\
	(This)->lpVtbl -> get_Size(This,pcrb)

#define ILgIcuResourceBundle_get_StringEx(This,irb,pbstr)	\
	(This)->lpVtbl -> get_StringEx(This,irb,pbstr)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_Init_Proxy(
	ILgIcuResourceBundle * This,
	/* [in] */ BSTR bstrPath,
	/* [in] */ BSTR locale);


void __RPC_STUB ILgIcuResourceBundle_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_Key_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ BSTR *pbstrKey);


void __RPC_STUB ILgIcuResourceBundle_get_Key_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_String_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ BSTR *pbstrString);


void __RPC_STUB ILgIcuResourceBundle_get_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_Name_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ BSTR *pbstrName);


void __RPC_STUB ILgIcuResourceBundle_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_GetSubsection_Proxy(
	ILgIcuResourceBundle * This,
	/* [in] */ BSTR bstrSectionName,
	/* [retval][out] */ ILgIcuResourceBundle **pprb);


void __RPC_STUB ILgIcuResourceBundle_get_GetSubsection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_HasNext_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ ComBool *pfHasNext);


void __RPC_STUB ILgIcuResourceBundle_get_HasNext_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_Next_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ ILgIcuResourceBundle **pprb);


void __RPC_STUB ILgIcuResourceBundle_get_Next_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_Size_Proxy(
	ILgIcuResourceBundle * This,
	/* [retval][out] */ int *pcrb);


void __RPC_STUB ILgIcuResourceBundle_get_Size_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ILgIcuResourceBundle_get_StringEx_Proxy(
	ILgIcuResourceBundle * This,
	/* [in] */ int irb,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ILgIcuResourceBundle_get_StringEx_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ILgIcuResourceBundle_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_LgSystemCollater;

#ifdef __cplusplus

class DECLSPEC_UUID("8D9C5BE3-03A8-11d3-8078-0000C0FB81B5")
LgSystemCollater;
#endif

EXTERN_C const CLSID CLSID_LgUnicodeCollater;

#ifdef __cplusplus

class DECLSPEC_UUID("8D9C5BE4-03A8-11d3-8078-0000C0FB81B5")
LgUnicodeCollater;
#endif

EXTERN_C const CLSID CLSID_LgIcuCollator;

#ifdef __cplusplus

class DECLSPEC_UUID("B076595F-EB05-4056-BF69-382B28521B10")
LgIcuCollator;
#endif

EXTERN_C const CLSID CLSID_LgCharacterPropertyEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("FC1C0D10-0483-11d3-8078-0000C0FB81B5")
LgCharacterPropertyEngine;
#endif

EXTERN_C const CLSID CLSID_LgIcuCharPropEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("7CD09B42-A981-4b3b-9815-C06654CF1F7D")
LgIcuCharPropEngine;
#endif

EXTERN_C const CLSID CLSID_LgCharPropOverrideEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("3BCA8781-182D-11d3-8078-0000C0FB81B5")
LgCharPropOverrideEngine;
#endif

EXTERN_C const CLSID CLSID_LgCPWordTokenizer;

#ifdef __cplusplus

class DECLSPEC_UUID("0D224004-03C7-11d3-8078-0000C0FB81B5")
LgCPWordTokenizer;
#endif

EXTERN_C const CLSID CLSID_LgWfiSpellChecker;

#ifdef __cplusplus

class DECLSPEC_UUID("FC1C0D03-0483-11d3-8078-0000C0FB81B5")
LgWfiSpellChecker;
#endif

EXTERN_C const CLSID CLSID_LgMSWordSpellChecker;

#ifdef __cplusplus

class DECLSPEC_UUID("3BCA8782-182D-11d3-8078-0000C0FB81B5")
LgMSWordSpellChecker;
#endif

EXTERN_C const CLSID CLSID_LgNumericEngine;

#ifdef __cplusplus

class DECLSPEC_UUID("FC1C0D08-0483-11d3-8078-0000C0FB81B5")
LgNumericEngine;
#endif

EXTERN_C const CLSID CLSID_LgWritingSystemFactory;

#ifdef __cplusplus

class DECLSPEC_UUID("D96B7867-EDE6-4c0d-80C6-B929300985A6")
LgWritingSystemFactory;
#endif

EXTERN_C const CLSID CLSID_LgWritingSystemFactoryBuilder;

#ifdef __cplusplus

class DECLSPEC_UUID("25D66955-3AE2-4b44-A6B1-0206EA5FE264")
LgWritingSystemFactoryBuilder;
#endif

EXTERN_C const CLSID CLSID_LgCollation;

#ifdef __cplusplus

class DECLSPEC_UUID("CF5077EC-7582-4330-87E6-EFAE05D9FC99")
LgCollation;
#endif

EXTERN_C const CLSID CLSID_LgWritingSystem;

#ifdef __cplusplus

class DECLSPEC_UUID("7EDD3897-B471-4aab-95E6-162C6DC0AC53")
LgWritingSystem;
#endif

EXTERN_C const CLSID CLSID_LgTsStringPlusWss;

#ifdef __cplusplus

class DECLSPEC_UUID("5289A9D3-E8D9-48f4-9AF7-E6014AA1E09C")
LgTsStringPlusWss;
#endif

EXTERN_C const CLSID CLSID_LgTsDataObject;

#ifdef __cplusplus

class DECLSPEC_UUID("1C0BB7A2-BADB-452b-ABA3-8E60C122A670")
LgTsDataObject;
#endif

EXTERN_C const CLSID CLSID_LgKeymanHandler;

#ifdef __cplusplus

class DECLSPEC_UUID("740C334A-76E7-4d78-AB39-48BEAE304DEC")
LgKeymanHandler;
#endif

EXTERN_C const CLSID CLSID_LgTextServices;

#ifdef __cplusplus

class DECLSPEC_UUID("2752634F-60F2-4065-B480-091A67C6033B")
LgTextServices;
#endif

EXTERN_C const CLSID CLSID_LgCodePageEnumerator;

#ifdef __cplusplus

class DECLSPEC_UUID("EF50E852-BA89-4014-A337-D1EF44AF0F35")
LgCodePageEnumerator;
#endif

EXTERN_C const CLSID CLSID_LgLanguageEnumerator;

#ifdef __cplusplus

class DECLSPEC_UUID("0629A66A-3877-40de-A27C-14BD51952BCF")
LgLanguageEnumerator;
#endif

EXTERN_C const CLSID CLSID_LgIcuConverterEnumerator;

#ifdef __cplusplus

class DECLSPEC_UUID("7583B4F0-9FA5-46df-A18B-B84DD12583CE")
LgIcuConverterEnumerator;
#endif

EXTERN_C const CLSID CLSID_LgIcuTransliteratorEnumerator;

#ifdef __cplusplus

class DECLSPEC_UUID("CC9CE163-8DA6-4f6a-B387-5F77CD683434")
LgIcuTransliteratorEnumerator;
#endif

EXTERN_C const CLSID CLSID_LgIcuResourceBundle;

#ifdef __cplusplus

class DECLSPEC_UUID("38B24B19-6CAB-4745-84DF-229EA8999D24")
LgIcuResourceBundle;
#endif

EXTERN_C const CLSID CLSID_LgIcuLocaleEnumerator;

#ifdef __cplusplus

class DECLSPEC_UUID("96C82FB7-B30A-4320-B1E7-A31951C0A30B")
LgIcuLocaleEnumerator;
#endif

#ifndef __IOleDbCommand_INTERFACE_DEFINED__
#define __IOleDbCommand_INTERFACE_DEFINED__

/* interface IOleDbCommand */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IOleDbCommand;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("21993161-3E24-11d4-A1BD-00C04F0C9593")
	IOleDbCommand : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ColValWasNull(
			/* [out] */ int *pfIsNull) = 0;

		virtual HRESULT STDMETHODCALLTYPE ExecCommand(
			/* [in] */ BSTR bstrSqlStatement,
			/* [in] */ int nStatementType) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetColValue(
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cbBufferLength,
			/* [out] */ ULONG *pcbAmtBuffUsed,
			/* [out] */ ComBool *pfIsNull,
			/* [in] */ int cbPad) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetInt(
			/* [in] */ int iColIndex,
			/* [out] */ int *pnValue) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetParameter(
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength,
			/* [out] */ ComBool *pfIsNull) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetRowset(
			/* [in] */ int nRowsBuffered) = 0;

		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IUnknown *punkSession,
			/* [in] */ IStream *pfistLog) = 0;

		virtual HRESULT STDMETHODCALLTYPE NextRow(
			/* [out] */ ComBool *pfMoreRows) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [in] */ WORD nDataType,
			/* [size_is][in] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetByteBuffParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ BYTE *prgbDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetStringParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ OLECHAR *prgchDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

	};

#else 	/* C style interface */

	typedef struct IOleDbCommandVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IOleDbCommand * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IOleDbCommand * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IOleDbCommand * This);

		HRESULT ( STDMETHODCALLTYPE *ColValWasNull )(
			IOleDbCommand * This,
			/* [out] */ int *pfIsNull);

		HRESULT ( STDMETHODCALLTYPE *ExecCommand )(
			IOleDbCommand * This,
			/* [in] */ BSTR bstrSqlStatement,
			/* [in] */ int nStatementType);

		HRESULT ( STDMETHODCALLTYPE *GetColValue )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cbBufferLength,
			/* [out] */ ULONG *pcbAmtBuffUsed,
			/* [out] */ ComBool *pfIsNull,
			/* [in] */ int cbPad);

		HRESULT ( STDMETHODCALLTYPE *GetInt )(
			IOleDbCommand * This,
			/* [in] */ int iColIndex,
			/* [out] */ int *pnValue);

		HRESULT ( STDMETHODCALLTYPE *GetParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength,
			/* [out] */ ComBool *pfIsNull);

		HRESULT ( STDMETHODCALLTYPE *GetRowset )(
			IOleDbCommand * This,
			/* [in] */ int nRowsBuffered);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IOleDbCommand * This,
			/* [in] */ IUnknown *punkSession,
			/* [in] */ IStream *pfistLog);

		HRESULT ( STDMETHODCALLTYPE *NextRow )(
			IOleDbCommand * This,
			/* [out] */ ComBool *pfMoreRows);

		HRESULT ( STDMETHODCALLTYPE *SetParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [in] */ WORD nDataType,
			/* [size_is][in] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		HRESULT ( STDMETHODCALLTYPE *SetByteBuffParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ BYTE *prgbDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		HRESULT ( STDMETHODCALLTYPE *SetStringParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ OLECHAR *prgchDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		END_INTERFACE
	} IOleDbCommandVtbl;

	interface IOleDbCommand
	{
		CONST_VTBL struct IOleDbCommandVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IOleDbCommand_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IOleDbCommand_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IOleDbCommand_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IOleDbCommand_ColValWasNull(This,pfIsNull)	\
	(This)->lpVtbl -> ColValWasNull(This,pfIsNull)

#define IOleDbCommand_ExecCommand(This,bstrSqlStatement,nStatementType)	\
	(This)->lpVtbl -> ExecCommand(This,bstrSqlStatement,nStatementType)

#define IOleDbCommand_GetColValue(This,iluColIndex,prgluDataBuffer,cbBufferLength,pcbAmtBuffUsed,pfIsNull,cbPad)	\
	(This)->lpVtbl -> GetColValue(This,iluColIndex,prgluDataBuffer,cbBufferLength,pcbAmtBuffUsed,pfIsNull,cbPad)

#define IOleDbCommand_GetInt(This,iColIndex,pnValue)	\
	(This)->lpVtbl -> GetInt(This,iColIndex,pnValue)

#define IOleDbCommand_GetParameter(This,iluColIndex,prgluDataBuffer,cluBufferLength,pfIsNull)	\
	(This)->lpVtbl -> GetParameter(This,iluColIndex,prgluDataBuffer,cluBufferLength,pfIsNull)

#define IOleDbCommand_GetRowset(This,nRowsBuffered)	\
	(This)->lpVtbl -> GetRowset(This,nRowsBuffered)

#define IOleDbCommand_Init(This,punkSession,pfistLog)	\
	(This)->lpVtbl -> Init(This,punkSession,pfistLog)

#define IOleDbCommand_NextRow(This,pfMoreRows)	\
	(This)->lpVtbl -> NextRow(This,pfMoreRows)

#define IOleDbCommand_SetParameter(This,iluParamIndex,dwFlags,bstrParamName,nDataType,prgluDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetParameter(This,iluParamIndex,dwFlags,bstrParamName,nDataType,prgluDataBuffer,cluBufferLength)

#define IOleDbCommand_SetByteBuffParameter(This,iluParamIndex,dwFlags,bstrParamName,prgbDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetByteBuffParameter(This,iluParamIndex,dwFlags,bstrParamName,prgbDataBuffer,cluBufferLength)

#define IOleDbCommand_SetStringParameter(This,iluParamIndex,dwFlags,bstrParamName,prgchDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetStringParameter(This,iluParamIndex,dwFlags,bstrParamName,prgchDataBuffer,cluBufferLength)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IOleDbCommand_ColValWasNull_Proxy(
	IOleDbCommand * This,
	/* [out] */ int *pfIsNull);


void __RPC_STUB IOleDbCommand_ColValWasNull_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_ExecCommand_Proxy(
	IOleDbCommand * This,
	/* [in] */ BSTR bstrSqlStatement,
	/* [in] */ int nStatementType);


void __RPC_STUB IOleDbCommand_ExecCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetColValue_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluColIndex,
	/* [size_is][out] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cbBufferLength,
	/* [out] */ ULONG *pcbAmtBuffUsed,
	/* [out] */ ComBool *pfIsNull,
	/* [in] */ int cbPad);


void __RPC_STUB IOleDbCommand_GetColValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetInt_Proxy(
	IOleDbCommand * This,
	/* [in] */ int iColIndex,
	/* [out] */ int *pnValue);


void __RPC_STUB IOleDbCommand_GetInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluColIndex,
	/* [size_is][out] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cluBufferLength,
	/* [out] */ ComBool *pfIsNull);


void __RPC_STUB IOleDbCommand_GetParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetRowset_Proxy(
	IOleDbCommand * This,
	/* [in] */ int nRowsBuffered);


void __RPC_STUB IOleDbCommand_GetRowset_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_Init_Proxy(
	IOleDbCommand * This,
	/* [in] */ IUnknown *punkSession,
	/* [in] */ IStream *pfistLog);


void __RPC_STUB IOleDbCommand_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_NextRow_Proxy(
	IOleDbCommand * This,
	/* [out] */ ComBool *pfMoreRows);


void __RPC_STUB IOleDbCommand_NextRow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [in] */ WORD nDataType,
	/* [size_is][in] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetByteBuffParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [size_is][in] */ BYTE *prgbDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetByteBuffParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetStringParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [size_is][in] */ OLECHAR *prgchDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetStringParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOleDbCommand_INTERFACE_DEFINED__ */


#ifndef __IFwMetaDataCache_INTERFACE_DEFINED__
#define __IFwMetaDataCache_INTERFACE_DEFINED__

/* interface IFwMetaDataCache */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwMetaDataCache;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("6AA9042E-0A4D-4f33-881B-3FBE48861D14")
	IFwMetaDataCache : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IOleDbEncap *pode) = 0;

		virtual HRESULT STDMETHODCALLTYPE Reload(
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ ComBool fKeepVirtuals) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitXml(
			/* [in] */ BSTR bstrPathname,
			/* [in] */ ComBool fClearPrevCache) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldCount(
			/* [retval][out] */ int *pcflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldIds(
			/* [in] */ int cflid,
			/* [size_is][out] */ ULONG *rgflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOwnClsName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrOwnClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDstClsName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrDstClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOwnClsId(
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluOwnClsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDstClsId(
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluDstClsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldLabel(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldLabel) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldHelp(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldHelp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldXml(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldXml) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldListRoot(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piListRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldWs(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piWs) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldType(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piType) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValidClass(
			/* [in] */ ULONG luFlid,
			/* [in] */ ULONG luClid,
			/* [retval][out] */ ComBool *pfValid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassCount(
			/* [retval][out] */ int *pcclid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassIds(
			/* [in] */ int cclid,
			/* [size_is][out] */ ULONG *rgclid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassName(
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrClassName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAbstract(
			/* [in] */ ULONG luClid,
			/* [out] */ ComBool *pfAbstract) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBaseClsId(
			/* [in] */ ULONG luClid,
			/* [out] */ ULONG *pluClid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBaseClsName(
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrBaseClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFields(
			/* [in] */ ULONG luClid,
			/* [in] */ ComBool fIncludeSuperclasses,
			/* [in] */ int grfcpt,
			/* [in] */ int cflidMax,
			/* [size_is][out] */ ULONG *prgflid,
			/* [out] */ int *pcflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassId(
			/* [in] */ BSTR bstrClassName,
			/* [retval][out] */ ULONG *pluClid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldId(
			/* [in] */ BSTR bstrClassName,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldId2(
			/* [in] */ ULONG luClid,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDirectSubclasses(
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAllSubclasses(
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddVirtualProp(
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [in] */ ULONG luFlid,
			/* [in] */ int type) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwMetaDataCacheVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwMetaDataCache * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwMetaDataCache * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwMetaDataCache * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IFwMetaDataCache * This,
			/* [in] */ IOleDbEncap *pode);

		HRESULT ( STDMETHODCALLTYPE *Reload )(
			IFwMetaDataCache * This,
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ ComBool fKeepVirtuals);

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
			/* [size_is][out] */ ULONG *rgflid);

		HRESULT ( STDMETHODCALLTYPE *GetOwnClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrOwnClsName);

		HRESULT ( STDMETHODCALLTYPE *GetDstClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrDstClsName);

		HRESULT ( STDMETHODCALLTYPE *GetOwnClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluOwnClsid);

		HRESULT ( STDMETHODCALLTYPE *GetDstClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluDstClsid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldName);

		HRESULT ( STDMETHODCALLTYPE *GetFieldLabel )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldLabel);

		HRESULT ( STDMETHODCALLTYPE *GetFieldHelp )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldHelp);

		HRESULT ( STDMETHODCALLTYPE *GetFieldXml )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldXml);

		HRESULT ( STDMETHODCALLTYPE *GetFieldListRoot )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piListRoot);

		HRESULT ( STDMETHODCALLTYPE *GetFieldWs )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piWs);

		HRESULT ( STDMETHODCALLTYPE *GetFieldType )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piType);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValidClass )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [in] */ ULONG luClid,
			/* [retval][out] */ ComBool *pfValid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassCount )(
			IFwMetaDataCache * This,
			/* [retval][out] */ int *pcclid);

		HRESULT ( STDMETHODCALLTYPE *GetClassIds )(
			IFwMetaDataCache * This,
			/* [in] */ int cclid,
			/* [size_is][out] */ ULONG *rgclid);

		HRESULT ( STDMETHODCALLTYPE *GetClassName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrClassName);

		HRESULT ( STDMETHODCALLTYPE *GetAbstract )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ ComBool *pfAbstract);

		HRESULT ( STDMETHODCALLTYPE *GetBaseClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ ULONG *pluClid);

		HRESULT ( STDMETHODCALLTYPE *GetBaseClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrBaseClsName);

		HRESULT ( STDMETHODCALLTYPE *GetFields )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ ComBool fIncludeSuperclasses,
			/* [in] */ int grfcpt,
			/* [in] */ int cflidMax,
			/* [size_is][out] */ ULONG *prgflid,
			/* [out] */ int *pcflid);

		HRESULT ( STDMETHODCALLTYPE *GetClassId )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClassName,
			/* [retval][out] */ ULONG *pluClid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldId )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClassName,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldId2 )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid);

		HRESULT ( STDMETHODCALLTYPE *GetDirectSubclasses )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses);

		HRESULT ( STDMETHODCALLTYPE *GetAllSubclasses )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses);

		HRESULT ( STDMETHODCALLTYPE *AddVirtualProp )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [in] */ ULONG luFlid,
			/* [in] */ int type);

		END_INTERFACE
	} IFwMetaDataCacheVtbl;

	interface IFwMetaDataCache
	{
		CONST_VTBL struct IFwMetaDataCacheVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwMetaDataCache_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwMetaDataCache_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwMetaDataCache_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwMetaDataCache_Init(This,pode)	\
	(This)->lpVtbl -> Init(This,pode)

#define IFwMetaDataCache_Reload(This,pode,fKeepVirtuals)	\
	(This)->lpVtbl -> Reload(This,pode,fKeepVirtuals)

#define IFwMetaDataCache_InitXml(This,bstrPathname,fClearPrevCache)	\
	(This)->lpVtbl -> InitXml(This,bstrPathname,fClearPrevCache)

#define IFwMetaDataCache_get_FieldCount(This,pcflid)	\
	(This)->lpVtbl -> get_FieldCount(This,pcflid)

#define IFwMetaDataCache_GetFieldIds(This,cflid,rgflid)	\
	(This)->lpVtbl -> GetFieldIds(This,cflid,rgflid)

#define IFwMetaDataCache_GetOwnClsName(This,luFlid,pbstrOwnClsName)	\
	(This)->lpVtbl -> GetOwnClsName(This,luFlid,pbstrOwnClsName)

#define IFwMetaDataCache_GetDstClsName(This,luFlid,pbstrDstClsName)	\
	(This)->lpVtbl -> GetDstClsName(This,luFlid,pbstrDstClsName)

#define IFwMetaDataCache_GetOwnClsId(This,luFlid,pluOwnClsid)	\
	(This)->lpVtbl -> GetOwnClsId(This,luFlid,pluOwnClsid)

#define IFwMetaDataCache_GetDstClsId(This,luFlid,pluDstClsid)	\
	(This)->lpVtbl -> GetDstClsId(This,luFlid,pluDstClsid)

#define IFwMetaDataCache_GetFieldName(This,luFlid,pbstrFieldName)	\
	(This)->lpVtbl -> GetFieldName(This,luFlid,pbstrFieldName)

#define IFwMetaDataCache_GetFieldLabel(This,luFlid,pbstrFieldLabel)	\
	(This)->lpVtbl -> GetFieldLabel(This,luFlid,pbstrFieldLabel)

#define IFwMetaDataCache_GetFieldHelp(This,luFlid,pbstrFieldHelp)	\
	(This)->lpVtbl -> GetFieldHelp(This,luFlid,pbstrFieldHelp)

#define IFwMetaDataCache_GetFieldXml(This,luFlid,pbstrFieldXml)	\
	(This)->lpVtbl -> GetFieldXml(This,luFlid,pbstrFieldXml)

#define IFwMetaDataCache_GetFieldListRoot(This,luFlid,piListRoot)	\
	(This)->lpVtbl -> GetFieldListRoot(This,luFlid,piListRoot)

#define IFwMetaDataCache_GetFieldWs(This,luFlid,piWs)	\
	(This)->lpVtbl -> GetFieldWs(This,luFlid,piWs)

#define IFwMetaDataCache_GetFieldType(This,luFlid,piType)	\
	(This)->lpVtbl -> GetFieldType(This,luFlid,piType)

#define IFwMetaDataCache_get_IsValidClass(This,luFlid,luClid,pfValid)	\
	(This)->lpVtbl -> get_IsValidClass(This,luFlid,luClid,pfValid)

#define IFwMetaDataCache_get_ClassCount(This,pcclid)	\
	(This)->lpVtbl -> get_ClassCount(This,pcclid)

#define IFwMetaDataCache_GetClassIds(This,cclid,rgclid)	\
	(This)->lpVtbl -> GetClassIds(This,cclid,rgclid)

#define IFwMetaDataCache_GetClassName(This,luClid,pbstrClassName)	\
	(This)->lpVtbl -> GetClassName(This,luClid,pbstrClassName)

#define IFwMetaDataCache_GetAbstract(This,luClid,pfAbstract)	\
	(This)->lpVtbl -> GetAbstract(This,luClid,pfAbstract)

#define IFwMetaDataCache_GetBaseClsId(This,luClid,pluClid)	\
	(This)->lpVtbl -> GetBaseClsId(This,luClid,pluClid)

#define IFwMetaDataCache_GetBaseClsName(This,luClid,pbstrBaseClsName)	\
	(This)->lpVtbl -> GetBaseClsName(This,luClid,pbstrBaseClsName)

#define IFwMetaDataCache_GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid)	\
	(This)->lpVtbl -> GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid)

#define IFwMetaDataCache_GetClassId(This,bstrClassName,pluClid)	\
	(This)->lpVtbl -> GetClassId(This,bstrClassName,pluClid)

#define IFwMetaDataCache_GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
	(This)->lpVtbl -> GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid)

#define IFwMetaDataCache_GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
	(This)->lpVtbl -> GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid)

#define IFwMetaDataCache_GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
	(This)->lpVtbl -> GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)

#define IFwMetaDataCache_GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
	(This)->lpVtbl -> GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)

#define IFwMetaDataCache_AddVirtualProp(This,bstrClass,bstrField,luFlid,type)	\
	(This)->lpVtbl -> AddVirtualProp(This,bstrClass,bstrField,luFlid,type)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwMetaDataCache_Init_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ IOleDbEncap *pode);


void __RPC_STUB IFwMetaDataCache_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_Reload_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ IOleDbEncap *pode,
	/* [in] */ ComBool fKeepVirtuals);


void __RPC_STUB IFwMetaDataCache_Reload_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_InitXml_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrPathname,
	/* [in] */ ComBool fClearPrevCache);


void __RPC_STUB IFwMetaDataCache_InitXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_FieldCount_Proxy(
	IFwMetaDataCache * This,
	/* [retval][out] */ int *pcflid);


void __RPC_STUB IFwMetaDataCache_get_FieldCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldIds_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ int cflid,
	/* [size_is][out] */ ULONG *rgflid);


void __RPC_STUB IFwMetaDataCache_GetFieldIds_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetOwnClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrOwnClsName);


void __RPC_STUB IFwMetaDataCache_GetOwnClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDstClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrDstClsName);


void __RPC_STUB IFwMetaDataCache_GetDstClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetOwnClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ ULONG *pluOwnClsid);


void __RPC_STUB IFwMetaDataCache_GetOwnClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDstClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ ULONG *pluDstClsid);


void __RPC_STUB IFwMetaDataCache_GetDstClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldName);


void __RPC_STUB IFwMetaDataCache_GetFieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldLabel_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldLabel);


void __RPC_STUB IFwMetaDataCache_GetFieldLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldHelp_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldHelp);


void __RPC_STUB IFwMetaDataCache_GetFieldHelp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldXml_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldXml);


void __RPC_STUB IFwMetaDataCache_GetFieldXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldListRoot_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piListRoot);


void __RPC_STUB IFwMetaDataCache_GetFieldListRoot_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldWs_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piWs);


void __RPC_STUB IFwMetaDataCache_GetFieldWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldType_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piType);


void __RPC_STUB IFwMetaDataCache_GetFieldType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_IsValidClass_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [in] */ ULONG luClid,
	/* [retval][out] */ ComBool *pfValid);


void __RPC_STUB IFwMetaDataCache_get_IsValidClass_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_ClassCount_Proxy(
	IFwMetaDataCache * This,
	/* [retval][out] */ int *pcclid);


void __RPC_STUB IFwMetaDataCache_get_ClassCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassIds_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ int cclid,
	/* [size_is][out] */ ULONG *rgclid);


void __RPC_STUB IFwMetaDataCache_GetClassIds_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ BSTR *pbstrClassName);


void __RPC_STUB IFwMetaDataCache_GetClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetAbstract_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ ComBool *pfAbstract);


void __RPC_STUB IFwMetaDataCache_GetAbstract_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetBaseClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ ULONG *pluClid);


void __RPC_STUB IFwMetaDataCache_GetBaseClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetBaseClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ BSTR *pbstrBaseClsName);


void __RPC_STUB IFwMetaDataCache_GetBaseClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFields_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ ComBool fIncludeSuperclasses,
	/* [in] */ int grfcpt,
	/* [in] */ int cflidMax,
	/* [size_is][out] */ ULONG *prgflid,
	/* [out] */ int *pcflid);


void __RPC_STUB IFwMetaDataCache_GetFields_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClassName,
	/* [retval][out] */ ULONG *pluClid);


void __RPC_STUB IFwMetaDataCache_GetClassId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClassName,
	/* [in] */ BSTR bstrFieldName,
	/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
	/* [retval][out] */ ULONG *pluFlid);


void __RPC_STUB IFwMetaDataCache_GetFieldId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldId2_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ BSTR bstrFieldName,
	/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
	/* [retval][out] */ ULONG *pluFlid);


void __RPC_STUB IFwMetaDataCache_GetFieldId2_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDirectSubclasses_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ int cluMax,
	/* [out] */ int *pcluOut,
	/* [length_is][size_is][out] */ ULONG *prgluSubclasses);


void __RPC_STUB IFwMetaDataCache_GetDirectSubclasses_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetAllSubclasses_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ int cluMax,
	/* [out] */ int *pcluOut,
	/* [length_is][size_is][out] */ ULONG *prgluSubclasses);


void __RPC_STUB IFwMetaDataCache_GetAllSubclasses_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_AddVirtualProp_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClass,
	/* [in] */ BSTR bstrField,
	/* [in] */ ULONG luFlid,
	/* [in] */ int type);


void __RPC_STUB IFwMetaDataCache_AddVirtualProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwMetaDataCache_INTERFACE_DEFINED__ */


#ifndef __IDbAdmin_INTERFACE_DEFINED__
#define __IDbAdmin_INTERFACE_DEFINED__

/* interface IDbAdmin */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IDbAdmin;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2A861F95-63D0-480d-B5AF-4FAF0D22125D")
	IDbAdmin : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CopyDatabase(
			/* [in] */ BSTR bstrSrcPathName,
			/* [in] */ BSTR bstrDstPathName) = 0;

		virtual HRESULT STDMETHODCALLTYPE AttachDatabase(
			/* [in] */ BSTR bstrDatabaseName,
			/* [in] */ BSTR bstrPathName) = 0;

		virtual HRESULT STDMETHODCALLTYPE DetachDatabase(
			/* [in] */ BSTR bstrDatabaseName) = 0;

		virtual HRESULT STDMETHODCALLTYPE RenameDatabase(
			/* [in] */ BSTR bstrDirName,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ BSTR bstrNewName,
			/* [in] */ ComBool fDetachBefore,
			/* [in] */ ComBool fAttachAfter) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_LogStream(
			/* [in] */ IStream *pstrm) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwRootDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwMigrationScriptDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwDatabaseDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwTemplateDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

	};

#else 	/* C style interface */

	typedef struct IDbAdminVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDbAdmin * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDbAdmin * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDbAdmin * This);

		HRESULT ( STDMETHODCALLTYPE *CopyDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrSrcPathName,
			/* [in] */ BSTR bstrDstPathName);

		HRESULT ( STDMETHODCALLTYPE *AttachDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDatabaseName,
			/* [in] */ BSTR bstrPathName);

		HRESULT ( STDMETHODCALLTYPE *DetachDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDatabaseName);

		HRESULT ( STDMETHODCALLTYPE *RenameDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDirName,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ BSTR bstrNewName,
			/* [in] */ ComBool fDetachBefore,
			/* [in] */ ComBool fAttachAfter);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_LogStream )(
			IDbAdmin * This,
			/* [in] */ IStream *pstrm);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwRootDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwMigrationScriptDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwDatabaseDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwTemplateDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		END_INTERFACE
	} IDbAdminVtbl;

	interface IDbAdmin
	{
		CONST_VTBL struct IDbAdminVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDbAdmin_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDbAdmin_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDbAdmin_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDbAdmin_CopyDatabase(This,bstrSrcPathName,bstrDstPathName)	\
	(This)->lpVtbl -> CopyDatabase(This,bstrSrcPathName,bstrDstPathName)

#define IDbAdmin_AttachDatabase(This,bstrDatabaseName,bstrPathName)	\
	(This)->lpVtbl -> AttachDatabase(This,bstrDatabaseName,bstrPathName)

#define IDbAdmin_DetachDatabase(This,bstrDatabaseName)	\
	(This)->lpVtbl -> DetachDatabase(This,bstrDatabaseName)

#define IDbAdmin_RenameDatabase(This,bstrDirName,bstrOldName,bstrNewName,fDetachBefore,fAttachAfter)	\
	(This)->lpVtbl -> RenameDatabase(This,bstrDirName,bstrOldName,bstrNewName,fDetachBefore,fAttachAfter)

#define IDbAdmin_putref_LogStream(This,pstrm)	\
	(This)->lpVtbl -> putref_LogStream(This,pstrm)

#define IDbAdmin_get_FwRootDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwRootDir(This,pbstr)

#define IDbAdmin_get_FwMigrationScriptDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwMigrationScriptDir(This,pbstr)

#define IDbAdmin_get_FwDatabaseDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwDatabaseDir(This,pbstr)

#define IDbAdmin_get_FwTemplateDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwTemplateDir(This,pbstr)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDbAdmin_CopyDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrSrcPathName,
	/* [in] */ BSTR bstrDstPathName);


void __RPC_STUB IDbAdmin_CopyDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_AttachDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDatabaseName,
	/* [in] */ BSTR bstrPathName);


void __RPC_STUB IDbAdmin_AttachDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_DetachDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDatabaseName);


void __RPC_STUB IDbAdmin_DetachDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_RenameDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDirName,
	/* [in] */ BSTR bstrOldName,
	/* [in] */ BSTR bstrNewName,
	/* [in] */ ComBool fDetachBefore,
	/* [in] */ ComBool fAttachAfter);


void __RPC_STUB IDbAdmin_RenameDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IDbAdmin_putref_LogStream_Proxy(
	IDbAdmin * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB IDbAdmin_putref_LogStream_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwRootDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwRootDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwMigrationScriptDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwMigrationScriptDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwDatabaseDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwDatabaseDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwTemplateDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwTemplateDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDbAdmin_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_OleDbEncap;

#ifdef __cplusplus

class DECLSPEC_UUID("AAB4A4A3-3C83-11d4-A1BB-00C04F0C9593")
OleDbEncap;
#endif

EXTERN_C const CLSID CLSID_FwMetaDataCache;

#ifdef __cplusplus

class DECLSPEC_UUID("3A1B1AC6-24C5-4ffe-85D5-675DB4B9FCBB")
FwMetaDataCache;
#endif

EXTERN_C const CLSID CLSID_DbAdmin;

#ifdef __cplusplus

class DECLSPEC_UUID("D584A725-8CF4-4699-941F-D1337AC7DB5C")
DbAdmin;
#endif
#endif /* __LanguageLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
