/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AfExportRes.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Resource definitions for AfExportDlg.  Note that these must be in the range given by
	kridExportMin and kridExportLim in AfCoreRes.h (24100-24200).
----------------------------------------------------------------------------------------------*/

#define kridExportDlg               24100
#define kctidExportType             24101
#define kctidExportSelectionOnly    24103
#define kcidExportFolder            24107
#define kctidExportFilename         24108
#define kctidExportBrowse           24109
#define kctidExportOpenWhenDone     24110

#define kstidExportMsgCaption           24120
#define kstidExportFileAlreadyFmt       24121
#define kstidExportingData              24122
#define kstidExportErrorMsgTitle        24123
#define kstidExportFolderNameFmt        24124
#define kstidExportErrorTitle           24125
#define kstidExportLaunchErrorMsg       24126
#define kstidExportFileAlreadyOpenFmt   24127
#define kstidExportErrorFilesGone       24128
#define kstidExportReallySlow           24129

#define kstidExportErrorXmlSyntax       24139
#define kstidExportErrorXmlProcess      24140
#define kstidExportFormattingData       24141
#define kstidExportFormattingDataPass   24142
#define kstidExportErrorTransforming    24143
#define kstidExportProcessStepFmt       24144

// Not really in the AfExport domain, but i didn't want to allocate a new block and create a
// new header file for just two values!
#define kstidMergeWrtSys        24198
#define kstidLogMergeWrtSys     24199
