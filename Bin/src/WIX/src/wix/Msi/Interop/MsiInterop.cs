//-------------------------------------------------------------------------------------------------
// <copyright file="MsiInterop.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// MsiInterop PInvoke code.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi.Interop
{
	using System;
	using System.Text;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Class exposing static functions and structs from MSI API.
	/// </summary>
	internal sealed class MsiInterop
	{
		// Component.Attributes
		internal const int MsidbComponentAttributesLocalOnly = 0;
		internal const int MsidbComponentAttributesSourceOnly = 1;
		internal const int MsidbComponentAttributesOptional = 2;
		internal const int MsidbComponentAttributesRegistryKeyPath = 4;
		internal const int MsidbComponentAttributesSharedDllRefCount = 8;
		internal const int MsidbComponentAttributesPermanent = 16;
		internal const int MsidbComponentAttributesODBCDataSource = 32;
		internal const int MsidbComponentAttributesTransitive = 64;
		internal const int MsidbComponentAttributesNeverOverwrite = 128;
		internal const int MsidbComponentAttributes64bit = 256;
		internal const int MsidbComponentAttributesDisableRegistryReflection = 512;

		// BBControl.Attributes & Control.Attributes
		internal const int MsidbControlAttributesVisible           = 0x00000001;
		internal const int MsidbControlAttributesEnabled           = 0x00000002;
		internal const int MsidbControlAttributesSunken            = 0x00000004;
		internal const int MsidbControlAttributesIndirect          = 0x00000008;
		internal const int MsidbControlAttributesInteger           = 0x00000010;
		internal const int MsidbControlAttributesRTLRO             = 0x00000020;
		internal const int MsidbControlAttributesRightAligned      = 0x00000040;
		internal const int MsidbControlAttributesLeftScroll        = 0x00000080;
		internal const int MsidbControlAttributesBiDi              = MsidbControlAttributesRTLRO | MsidbControlAttributesRightAligned | MsidbControlAttributesLeftScroll;
		// Text controls
		internal const int MsidbControlAttributesTransparent       = 0x00010000;
		internal const int MsidbControlAttributesNoPrefix          = 0x00020000;
		internal const int MsidbControlAttributesNoWrap            = 0x00040000;
		internal const int MsidbControlAttributesFormatSize        = 0x00080000;
		internal const int MsidbControlAttributesUsersLanguage     = 0x00100000;
		// Edit controls
		internal const int MsidbControlAttributesMultiline         = 0x00010000;
		internal const int MsidbControlAttributesPasswordInput     = 0x00200000;
		// ProgressBar controls
		internal const int MsidbControlAttributesProgress95        = 0x00010000;
		// VolumeSelectCombo and DirectoryCombo controls
		internal const int MsidbControlAttributesRemovableVolume   = 0x00010000;
		internal const int MsidbControlAttributesFixedVolume       = 0x00020000;
		internal const int MsidbControlAttributesRemoteVolume      = 0x00040000;
		internal const int MsidbControlAttributesCDROMVolume       = 0x00080000;
		internal const int MsidbControlAttributesRAMDiskVolume     = 0x00100000;
		internal const int MsidbControlAttributesFloppyVolume      = 0x00200000;
		// VolumeCostList controls
		internal const int MsidbControlShowRollbackCost            = 0x00400000;
		// ListBox and ComboBox controls
		internal const int MsidbControlAttributesSorted            = 0x00010000;
		internal const int MsidbControlAttributesComboList         = 0x00020000;
		// picture button controls
		internal const int MsidbControlAttributesImageHandle       = 0x00010000;
		internal const int MsidbControlAttributesPushLike          = 0x00020000;
		internal const int MsidbControlAttributesBitmap            = 0x00040000;
		internal const int MsidbControlAttributesIcon              = 0x00080000;
		internal const int MsidbControlAttributesFixedSize         = 0x00100000;
		internal const int MsidbControlAttributesIconSize16        = 0x00200000;
		internal const int MsidbControlAttributesIconSize32        = 0x00400000;
		internal const int MsidbControlAttributesIconSize48        = 0x00600000;
		internal const int MsidbControlAttributesElevationShield   = 0x00800000;

		// RadioButton controls
		internal const int MsidbControlAttributesHasBorder         = 0x01000000;

		// CustomAction.Type
		// executable types
		internal const int MsidbCustomActionTypeDll              = 0x00000001;  // Target = entry point name
		internal const int MsidbCustomActionTypeExe              = 0x00000002;  // Target = command line args
		internal const int MsidbCustomActionTypeTextData         = 0x00000003;  // Target = text string to be formatted and set into property
		internal const int MsidbCustomActionTypeJScript          = 0x00000005;  // Target = entry point name; null if none to call
		internal const int MsidbCustomActionTypeVBScript         = 0x00000006;  // Target = entry point name; null if none to call
		internal const int MsidbCustomActionTypeInstall          = 0x00000007;  // Target = property list for nested engine initialization
		internal const int MsidbCustomActionTypeSourceBits       = 0x00000030;
		internal const int MsidbCustomActionTypeTypeBits         = 0x00000007;
		internal const int MsidbCustomActionTypeReturnBits       = 0x000000C0;
		internal const int MsidbCustomActionTypeExecuteBits      = 0x00000700;
		// source of code
		internal const int MsidbCustomActionTypeBinaryData       = 0x00000000;  // Source = Binary.Name; data stored in stream
		internal const int MsidbCustomActionTypeSourceFile       = 0x00000010;  // Source = File.File; file part of installation
		internal const int MsidbCustomActionTypeDirectory        = 0x00000020;  // Source = Directory.Directory; folder containing existing file
		internal const int MsidbCustomActionTypeProperty         = 0x00000030;  // Source = Property.Property; full path to executable
		// return processing; default is syncronous execution; process return code
		internal const int MsidbCustomActionTypeContinue         = 0x00000040;  // ignore action return status; continue running
		internal const int MsidbCustomActionTypeAsync            = 0x00000080;  // run asynchronously
		// execution scheduling flags; default is execute whenever sequenced
		internal const int MsidbCustomActionTypeFirstSequence    = 0x00000100;  // skip if UI sequence already run
		internal const int MsidbCustomActionTypeOncePerProcess   = 0x00000200;  // skip if UI sequence already run in same process
		internal const int MsidbCustomActionTypeClientRepeat     = 0x00000300;  // run on client only if UI already run on client
		internal const int MsidbCustomActionTypeInScript         = 0x00000400;  // queue for execution within script
		internal const int MsidbCustomActionTypeRollback         = 0x00000100;  // in conjunction with InScript: queue in Rollback script
		internal const int MsidbCustomActionTypeCommit           = 0x00000200;  // in conjunction with InScript: run Commit ops from script on success
		// security context flag; default to impersonate as user; valid only if InScript
		internal const int MsidbCustomActionTypeNoImpersonate    = 0x00000800;  // no impersonation; run in system context
		internal const int MsidbCustomActionTypeTSAware          = 0x00004000;  // impersonate for per-machine installs on TS machines
		internal const int MsidbCustomActionType64BitScript      = 0x00001000;  // script should run in 64bit process
		internal const int MsidbCustomActionTypeHideTarget       = 0x00002000;  // don't record the contents of the Target field in the log file.

		// Dialog.Attributes
		internal const int MsidbDialogAttributesVisible          = 0x00000001;
		internal const int MsidbDialogAttributesModal            = 0x00000002;
		internal const int MsidbDialogAttributesMinimize         = 0x00000004;
		internal const int MsidbDialogAttributesSysModal         = 0x00000008;
		internal const int MsidbDialogAttributesKeepModeless     = 0x00000010;
		internal const int MsidbDialogAttributesTrackDiskSpace   = 0x00000020;
		internal const int MsidbDialogAttributesUseCustomPalette = 0x00000040;
		internal const int MsidbDialogAttributesRTLRO            = 0x00000080;
		internal const int MsidbDialogAttributesRightAligned     = 0x00000100;
		internal const int MsidbDialogAttributesLeftScroll       = 0x00000200;
		internal const int MsidbDialogAttributesBiDi             = MsidbDialogAttributesRTLRO | MsidbDialogAttributesRightAligned | MsidbDialogAttributesLeftScroll;
		internal const int MsidbDialogAttributesError            = 0x00010000;
		internal const int CommonControlAttributesInvert         = MsidbControlAttributesVisible + MsidbControlAttributesEnabled;
		internal const int DialogAttributesInvert                = MsidbDialogAttributesVisible + MsidbDialogAttributesModal + MsidbDialogAttributesMinimize;
		// Feature.Attributes
		internal const int MsidbFeatureAttributesFavorLocal = 0;
		internal const int MsidbFeatureAttributesFavorSource = 1;
		internal const int MsidbFeatureAttributesFollowParent = 2;
		internal const int MsidbFeatureAttributesFavorAdvertise = 4;
		internal const int MsidbFeatureAttributesDisallowAdvertise = 8;
		internal const int MsidbFeatureAttributesUIDisallowAbsent = 16;
		internal const int MsidbFeatureAttributesNoUnsupportedAdvertise = 32;

		// File.Attributes
		internal const int MsidbFileAttributesReadOnly = 1;
		internal const int MsidbFileAttributesHidden = 2;
		internal const int MsidbFileAttributesSystem = 4;
		internal const int MsidbFileAttributesVital = 512;
		internal const int MsidbFileAttributesChecksum = 1024;
		internal const int MsidbFileAttributesPatchAdded = 4096;
		internal const int MsidbFileAttributesNoncompressed = 8192;
		internal const int MsidbFileAttributesCompressed = 16384;

		// IniFile.Action & RemoveIniFile.Action
		internal const int MsidbIniFileActionAddLine    = 0;
		internal const int MsidbIniFileActionCreateLine = 1;
		internal const int MsidbIniFileActionRemoveLine = 2;
		internal const int MsidbIniFileActionAddTag     = 3;
		internal const int MsidbIniFileActionRemoveTag  = 4;

		// ServiceInstall.Attributes
		internal const int MsidbServiceInstallOwnProcess        = 0x00000010;
		internal const int MsidbServiceInstallShareProcess      = 0x00000020;
		internal const int MsidbServiceInstallInteractive       = 0x00000100;
		internal const int MsidbServiceInstallAutoStart         = 0x00000002;
		internal const int MsidbServiceInstallDemandStart       = 0x00000003;
		internal const int MsidbServiceInstallDisabled          = 0x00000004;
		internal const int MsidbServiceInstallErrorIgnore       = 0x00000000;
		internal const int MsidbServiceInstallErrorNormal       = 0x00000001;
		internal const int MsidbServiceInstallErrorCritical     = 0x00000003;
		internal const int MsidbServiceInstallErrorControlVital = 0x00008000;

		// ServiceControl.Attributes
		internal const int MsidbServiceControlEventStart           = 0x00000001;
		internal const int MsidbServiceControlEventStop            = 0x00000002;
		internal const int MsidbServiceControlEventDelete          = 0x00000008;
		internal const int MsidbServiceControlEventUninstallStart  = 0x00000010;
		internal const int MsidbServiceControlEventUninstallStop   = 0x00000020;
		internal const int MsidbServiceControlEventUninstallDelete = 0x00000080;

		// Upgrade.Attributes
		internal const int MsidbUpgradeAttributesMigrateFeatures     = 0x00000001;
		internal const int MsidbUpgradeAttributesOnlyDetect          = 0x00000002;
		internal const int MsidbUpgradeAttributesIgnoreRemoveFailure = 0x00000008;
		internal const int MsidbUpgradeAttributesVersionMinInclusive = 0x00000100;
		internal const int MsidbUpgradeAttributesVersionMaxInclusive = 0x00000200;
		internal const int MsidbUpgradeAttributesLanguagesExclusive  = 0x00000400;

		// Registry Hive Roots
		internal const int MsidbRegistryRootClassesRoot = 0;
		internal const int MsidbRegistryRootCurrentUser = 1;
		internal const int MsidbRegistryRootLocalMachine = 2;
		internal const int MsidbRegistryRootUsers = 3;

		// Locator Types
		internal const int MsidbLocatorTypeDirectory = 0;
		internal const int MsidbLocatorTypeFileName = 1;
		internal const int MsidbLocatorTypeRawValue = 2;

		internal const int MsidbClassAttributesRelativePath = 1;

		// RemoveFile.InstallMode
		internal const int MsidbRemoveFileInstallModeOnInstall = 0x00000001;
		internal const int MsidbRemoveFileInstallModeOnRemove  = 0x00000002;
		internal const int MsidbRemoveFileInstallModeOnBoth    = 0x00000003;

		// ODBCDataSource.Registration
		internal const int MsidbODBCDataSourceRegistrationPerMachine = 0;
		internal const int MsidbODBCDataSourceRegistrationPerUser    = 1;

		// ModuleConfiguration.Format
		internal const int MsidbModuleConfigurationFormatText = 0;
		internal const int MsidbModuleConfigurationFormatKey = 1;
		internal const int MsidbModuleConfigurationFormatInteger = 2;
		internal const int MsidbModuleConfigurationFormatBitfield = 3;

		// ModuleConfiguration.Attributes
		internal const int MsidbMsmConfigurableOptionKeyNoOrphan = 1;
		internal const int MsidbMsmConfigurableOptionNonNullable = 2;

		// ' Windows API function ShowWindow constants - used in Shortcut table
		internal const int SWSHOWNORMAL                         = 0x00000001;
		internal const int SWSHOWMAXIMIZED                      = 0x00000003;
		internal const int SWSHOWMINNOACTIVE                    = 0x00000007;

		// NameToBit arrays
		// UI elements
		internal static readonly string[] DialogAttributes = { "Hidden", "Modeless", "NoMinimize", "SystemModal", "KeepModeless", "TrackDiskSpace", "CustomPalette", "RightToLeft", "RightAligned", "LeftScroll" };
		internal static readonly string[] CommonControlAttributes = { "Hidden", "Disabled", "Sunken", "Indirect", "Integer", "RightToLeft", "RightAligned", "LeftScroll" };
		internal static readonly string[] TextControlAttributes = { "Transparent", "NoPrefix", "NoWrap", "FormatSize", "UserLanguage" };
		internal static readonly string[] EditControlAttributes = { "Multiline", null, null, null,    null, "Password" };
		internal static readonly string[] ProgressControlAttributes = { "ProgressBlocks" };
		internal static readonly string[] VolumeControlAttributes = { "Removable", "Fixed", "Remote", "CDROM", "RAMDisk", "Floppy", "ShowRollbackCost" };
		internal static readonly string[] ListboxControlAttributes = { "Sorted", null, null, null, "UserLanguage" };
		internal static readonly string[] ListviewControlAttributes = { "Sorted", null, null, null, "FixedSize", "Icon16", "Icon32" };
		internal static readonly string[] ComboboxControlAttributes = { "Sorted", "ComboList", null, null, "UserLanguage" };
		internal static readonly string[] RadioControlAttributes = { "Image", "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", null, "HasBorder" };
		internal static readonly string[] ButtonControlAttributes = { "Image", null, "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", "ElevationShield" };
		internal static readonly string[] IconControlAttributes = { "Image", null, null, null, "FixedSize", "Icon16", "Icon32" };
		internal static readonly string[] BitmapControlAttributes = { "Image", null, null, null, "FixedSize" };
		internal static readonly string[] CheckboxControlAttributes = { null, "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32" };
		// Boolean permission to bit translation
		internal static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };
		internal static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };
		internal static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", null, "ReadAttributes", "WriteAttributes" };
		internal static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };
		internal static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };
		internal static readonly string[] ServicePermissions = { "ServiceQueryConfig", "ServiceChangeConfig", "ServiceQueryStatus", "ServiceEnumerateDependents", "ServiceStart", "ServiceStop", "ServicePauseContinue", "ServiceInterrogate", "ServiceUserDefinedControl" };

		internal const int MSICONDITIONFALSE = 0;   // The table is temporary.
		internal const int MSICONDITIONTRUE = 1;   // The table is persistent.
		internal const int MSICONDITIONNONE = 2;   // The table is unknown.
		internal const int MSICONDITIONERROR = 3;   // An invalid handle or invalid parameter was passed to the function.

		internal const int MSIDBOPENREADONLY = 0;
		internal const int MSIDBOPENTRANSACT = 1;
		internal const int MSIDBOPENDIRECT = 2;
		internal const int MSIDBOPENCREATE = 3;
		internal const int MSIDBOPENCREATEDIRECT = 4;
		internal const int MSIDBOPENPATCHFILE = 4;

		internal const int MSIMODIFYSEEK = -1;   // Refreshes the information in the supplied record without changing the position in the result set and without affecting subsequent fetch operations. The record may then be used for subsequent Update, Delete, and Refresh. All primary key columns of the table must be in the query and the record must have at least as many fields as the query. Seek cannot be used with multi-table queries. This mode cannot be used with a view containing joins. See also the remarks.
		internal const int MSIMODIFYREFRESH = 0;   // Refreshes the information in the record. Must first call MsiViewFetch with the same record. Fails for a deleted row. Works with read-write and read-only records.
		internal const int MSIMODIFYINSERT = 1;   // Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only database. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYUPDATE = 2;   // Updates an existing record. Nonprimary keys only. Must first call MsiViewFetch. Fails with a deleted record. Works only with read-write records.
		internal const int MSIMODIFYASSIGN = 3;   // Writes current data in the cursor to a table row. Updates record if the primary keys match an existing row and inserts if they do not match. Fails with a read-only database. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYREPLACE = 4;   // Updates or deletes and inserts a record into a table. Must first call MsiViewFetch with the same record. Updates record if the primary keys are unchanged. Deletes old row and inserts new if primary keys have changed. Fails with a read-only database. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYMERGE = 5;   // Inserts or validates a record in a table. Inserts if primary keys do not match any row and validates if there is a match. Fails if the record does not match the data in the table. Fails if there is a record with a duplicate key that is not identical. Works only with read-write records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYDELETE = 6;   // Remove a row from the table. You must first call the MsiViewFetch function with the same record. Fails if the row has been deleted. Works only with read-write records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYINSERTTEMPORARY = 7;   // Inserts a temporary record. The information is not persistent. Fails if a row with the same primary key exists. Works only with read-write records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYVALIDATE = 8;   // Validates a record. Does not validate across joins. You must first call the MsiViewFetch function with the same record. Obtain validation errors with MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYVALIDATENEW = 9;   // Validate a new record. Does not validate across joins. Checks for duplicate keys. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYVALIDATEFIELD = 10;   // Validates fields of a fetched or new record. Can validate one or more fields of an incomplete record. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
		internal const int MSIMODIFYVALIDATEDELETE = 11;   // Validates a record that will be deleted later. You must first call MsiViewFetch. Fails if another row refers to the primary keys of this row. Validation does not check for the existence of the primary keys of this row in properties or strings. Does not check if a column is a foreign key to multiple tables. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.

		internal const uint VTI2 = 2;
		internal const uint VTI4 = 3;
		internal const uint VTLPWSTR = 30;
		internal const uint VTFILETIME = 64;

		internal const int MSICOLINFONAMES = 0;  // return column names
		internal const int MSICOLINFOTYPES = 1;  // return column definitions, datatype code followed by width

		/// <summary>
		/// Record Indices into Msi Table ActionText
		/// </summary>
		internal enum ActionText
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ActionText</summary>
			Action = 1,
			/// <summary>Index to column name Description into Record for row in Msi Table ActionText</summary>
			Description = 2,
			/// <summary>Index to column name Template into Record for row in Msi Table ActionText</summary>
			Template = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table AdminExecuteSequence
		/// </summary>
		internal enum AdminExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table AdminExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table AdminExecuteSequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table AdminExecuteSequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table AdminUISequence
		/// </summary>
		internal enum AdminUISequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table AdminUISequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table AdminUISequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table AdminUISequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table AdvtExecuteSequence
		/// </summary>
		internal enum AdvtExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table AdvtExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table AdvtExecuteSequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table AdvtExecuteSequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table AdvtUISequence
		/// </summary>
		internal enum AdvtUISequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table AdvtUISequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table AdvtUISequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table AdvtUISequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table AppId
		/// </summary>
		internal enum AppId
		{
			/// <summary>Index to column name AppId into Record for row in Msi Table AppId</summary>
			AppId = 1,
			/// <summary>Index to column name RemoteServerName into Record for row in Msi Table AppId</summary>
			RemoteServerName = 2,
			/// <summary>Index to column name LocalService into Record for row in Msi Table AppId</summary>
			LocalService = 3,
			/// <summary>Index to column name ServiceParameters into Record for row in Msi Table AppId</summary>
			ServiceParameters = 4,
			/// <summary>Index to column name DllSurrogate into Record for row in Msi Table AppId</summary>
			DllSurrogate = 5,
			/// <summary>Index to column name ActivateAtStorage into Record for row in Msi Table AppId</summary>
			ActivateAtStorage = 6,
			/// <summary>Index to column name RunAsInteractiveUser into Record for row in Msi Table AppId</summary>
			RunAsInteractiveUser = 7,
		}

		/// <summary>
		/// Record Indices into Msi Table AppSearch
		/// </summary>
		internal enum AppSearch
		{
			/// <summary>Index to column name Property into Record for row in Msi Table AppSearch</summary>
			Property = 1,
			/// <summary>Index to column name Signature into Record for row in Msi Table AppSearch</summary>
			Signature = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table BBControl
		/// </summary>
		internal enum BBControl
		{
			/// <summary>Index to column name Billboard into Record for row in Msi Table BBControl</summary>
			Billboard = 1,
			/// <summary>Index to column name BBControl into Record for row in Msi Table BBControl</summary>
			BBControl = 2,
			/// <summary>Index to column name Type into Record for row in Msi Table BBControl</summary>
			Type = 3,
			/// <summary>Index to column name X into Record for row in Msi Table BBControl</summary>
			X = 4,
			/// <summary>Index to column name Y into Record for row in Msi Table BBControl</summary>
			Y = 5,
			/// <summary>Index to column name Width into Record for row in Msi Table BBControl</summary>
			Width = 6,
			/// <summary>Index to column name Height into Record for row in Msi Table BBControl</summary>
			Height = 7,
			/// <summary>Index to column name Attributes into Record for row in Msi Table BBControl</summary>
			Attributes = 8,
			/// <summary>Index to column name Text into Record for row in Msi Table BBControl</summary>
			Text = 9,
		}

		/// <summary>
		/// Record Indices into Msi Table Billboard
		/// </summary>
		internal enum Billboard
		{
			/// <summary>Index to column name Billboard into Record for row in Msi Table Billboard</summary>
			Billboard = 1,
			/// <summary>Index to column name Feature into Record for row in Msi Table Billboard</summary>
			Feature = 2,
			/// <summary>Index to column name Action into Record for row in Msi Table Billboard</summary>
			Action = 3,
			/// <summary>Index to column name Ordering into Record for row in Msi Table Billboard</summary>
			Ordering = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table Binary
		/// </summary>
		internal enum Binary
		{
			/// <summary>Index to column name Name into Record for row in Msi Table Binary</summary>
			Name = 1,
			/// <summary>Index to column name Data into Record for row in Msi Table Binary</summary>
			Data = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table BindImage
		/// </summary>
		internal enum BindImage
		{
			/// <summary>Index to column name File into Record for row in Msi Table BindImage</summary>
			File = 1,
			/// <summary>Index to column name Path into Record for row in Msi Table BindImage</summary>
			Path = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table CCPSearch
		/// </summary>
		internal enum CCPSearch
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table CCPSearch</summary>
			Signature = 1,
		}

		/// <summary>
		/// Record Indices into Msi Table CheckBox
		/// </summary>
		internal enum CheckBox
		{
			/// <summary>Index to column name Property into Record for row in Msi Table CheckBox</summary>
			Property = 1,
			/// <summary>Index to column name Value into Record for row in Msi Table CheckBox</summary>
			Value = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table Class
		/// </summary>
		internal enum Class
		{
			/// <summary>Index to column name CLSID into Record for row in Msi Table Class</summary>
			CLSID = 1,
			/// <summary>Index to column name Context into Record for row in Msi Table Class</summary>
			Context = 2,
			/// <summary>Index to column name Component into Record for row in Msi Table Class</summary>
			Component = 3,
			/// <summary>Index to column name ProgIdDefault into Record for row in Msi Table Class</summary>
			ProgId = 4,
			/// <summary>Index to column name Description into Record for row in Msi Table Class</summary>
			Description = 5,
			/// <summary>Index to column name AppId into Record for row in Msi Table Class</summary>
			AppId = 6,
			/// <summary>Index to column name FileTypeMask into Record for row in Msi Table Class</summary>
			FileTypeMask = 7,
			/// <summary>Index to column name Icon into Record for row in Msi Table Class</summary>
			Icon = 8,
			/// <summary>Index to column name IconIndex into Record for row in Msi Table Class</summary>
			IconIndex = 9,
			/// <summary>Index to column name DefInprocHandler into Record for row in Msi Table Class</summary>
			DefInprocHandler = 10,
			/// <summary>Index to column name Argument into Record for row in Msi Table Class</summary>
			Argument = 11,
			/// <summary>Index to column name Feature into Record for row in Msi Table Class</summary>
			Feature = 12,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Class</summary>
			Attributes = 13,
		}

		/// <summary>
		/// Record Indices into Msi Table ComboBox
		/// </summary>
		internal enum ComboBox
		{
			/// <summary>Index to column name Property into Record for row in Msi Table ComboBox</summary>
			Property = 1,
			/// <summary>Index to column name Order into Record for row in Msi Table ComboBox</summary>
			Order = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table ComboBox</summary>
			Value = 3,
			/// <summary>Index to column name Text into Record for row in Msi Table ComboBox</summary>
			Text = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table CompLocator
		/// </summary>
		internal enum CompLocator
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table CompLocator</summary>
			Signature = 1,
			/// <summary>Index to column name ComponentId into Record for row in Msi Table CompLocator</summary>
			ComponentId = 2,
			/// <summary>Index to column name Type into Record for row in Msi Table CompLocator</summary>
			Type = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table Complus
		/// </summary>
		internal enum Complus
		{
			/// <summary>Index to column name Component into Record for row in Msi Table Complus</summary>
			Component = 1,
			/// <summary>Index to column name ExpType into Record for row in Msi Table Complus</summary>
			ExpType = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table Component
		/// </summary>
		internal enum Component
		{
			/// <summary>Index to column name Component into Record for row in Msi Table Component</summary>
			Component = 1,
			/// <summary>Index to column name ComponentId into Record for row in Msi Table Component</summary>
			ComponentId = 2,
			/// <summary>Index to column name Directory into Record for row in Msi Table Component</summary>
			Directory = 3,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Component</summary>
			Attributes = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table Component</summary>
			Condition = 5,
			/// <summary>Index to column name KeyPath into Record for row in Msi Table Component</summary>
			KeyPath = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table Condition
		/// </summary>
		internal enum Condition
		{
			/// <summary>Index to column name Feature into Record for row in Msi Table Condition</summary>
			Feature = 1,
			/// <summary>Index to column name Level into Record for row in Msi Table Condition</summary>
			Level = 2,
			/// <summary>Index to column name Condition into Record for row in Msi Table Condition</summary>
			Condition = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table Control
		/// </summary>
		internal enum Control
		{
			/// <summary>Index to column name Dialog into Record for row in Msi Table Control</summary>
			Dialog = 1,
			/// <summary>Index to column name Control into Record for row in Msi Table Control</summary>
			Control = 2,
			/// <summary>Index to column name Type into Record for row in Msi Table Control</summary>
			Type = 3,
			/// <summary>Index to column name X into Record for row in Msi Table Control</summary>
			X = 4,
			/// <summary>Index to column name Y into Record for row in Msi Table Control</summary>
			Y = 5,
			/// <summary>Index to column name Width into Record for row in Msi Table Control</summary>
			Width = 6,
			/// <summary>Index to column name Height into Record for row in Msi Table Control</summary>
			Height = 7,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Control</summary>
			Attributes = 8,
			/// <summary>Index to column name Property into Record for row in Msi Table Control</summary>
			Property = 9,
			/// <summary>Index to column name Text into Record for row in Msi Table Control</summary>
			Text = 10,
			/// <summary>Index to column name ControlNext into Record for row in Msi Table Control</summary>
			ControlNext = 11,
			/// <summary>Index to column name Help into Record for row in Msi Table Control</summary>
			Help = 12,
		}

		/// <summary>
		/// Record Indices into Msi Table ControlCondition
		/// </summary>
		internal enum ControlCondition
		{
			/// <summary>Index to column name Dialog into Record for row in Msi Table ControlCondition</summary>
			Dialog = 1,
			/// <summary>Index to column name Control into Record for row in Msi Table ControlCondition</summary>
			Control = 2,
			/// <summary>Index to column name Action into Record for row in Msi Table ControlCondition</summary>
			Action = 3,
			/// <summary>Index to column name Condition into Record for row in Msi Table ControlCondition</summary>
			Condition = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table ControlEvent
		/// </summary>
		internal enum ControlEvent
		{
			/// <summary>Index to column name Dialog into Record for row in Msi Table ControlEvent</summary>
			Dialog = 1,
			/// <summary>Index to column name Control into Record for row in Msi Table ControlEvent</summary>
			Control = 2,
			/// <summary>Index to column name Event into Record for row in Msi Table ControlEvent</summary>
			Event = 3,
			/// <summary>Index to column name Argument into Record for row in Msi Table ControlEvent</summary>
			Argument = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ControlEvent</summary>
			Condition = 5,
			/// <summary>Index to column name Ordering into Record for row in Msi Table ControlEvent</summary>
			Ordering = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table CreateFolder
		/// </summary>
		internal enum CreateFolder
		{
			/// <summary>Index to column name Directory into Record for row in Msi Table CreateFolder</summary>
			Directory = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table CreateFolder</summary>
			Component = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table CustomAction
		/// </summary>
		internal enum CustomAction
		{
			/// <summary>Index to column name Action into Record for row in Msi Table CustomAction</summary>
			Action = 1,
			/// <summary>Index to column name Type into Record for row in Msi Table CustomAction</summary>
			Type = 2,
			/// <summary>Index to column name Source into Record for row in Msi Table CustomAction</summary>
			Source = 3,
			/// <summary>Index to column name Target into Record for row in Msi Table CustomAction</summary>
			Target = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table Dialog
		/// </summary>
		internal enum Dialog
		{
			/// <summary>Index to column name Dialog into Record for row in Msi Table Dialog</summary>
			Dialog = 1,
			/// <summary>Index to column name HCentering into Record for row in Msi Table Dialog</summary>
			HCentering = 2,
			/// <summary>Index to column name VCentering into Record for row in Msi Table Dialog</summary>
			VCentering = 3,
			/// <summary>Index to column name Width into Record for row in Msi Table Dialog</summary>
			Width = 4,
			/// <summary>Index to column name Height into Record for row in Msi Table Dialog</summary>
			Height = 5,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Dialog</summary>
			Attributes = 6,
			/// <summary>Index to column name Title into Record for row in Msi Table Dialog</summary>
			Title = 7,
			/// <summary>Index to column name ControlFirst into Record for row in Msi Table Dialog</summary>
			ControlFirst = 8,
			/// <summary>Index to column name ControlDefault into Record for row in Msi Table Dialog</summary>
			ControlDefault = 9,
			/// <summary>Index to column name ControlCancel into Record for row in Msi Table Dialog</summary>
			ControlCancel = 10,
		}

		/// <summary>
		/// Record Indices into Msi Table Directory
		/// </summary>
		internal enum Directory
		{
			/// <summary>Index to column name Directory into Record for row in Msi Table Directory</summary>
			Directory = 1,
			/// <summary>Index to column name DirectoryParent into Record for row in Msi Table Directory</summary>
			DirectoryParent = 2,
			/// <summary>Index to column name DefaultDir into Record for row in Msi Table Directory</summary>
			DefaultDir = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table DrLocator
		/// </summary>
		internal enum DrLocator
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table DrLocator</summary>
			Signature = 1,
			/// <summary>Index to column name Parent into Record for row in Msi Table DrLocator</summary>
			Parent = 2,
			/// <summary>Index to column name Path into Record for row in Msi Table DrLocator</summary>
			Path = 3,
			/// <summary>Index to column name Depth into Record for row in Msi Table DrLocator</summary>
			Depth = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table DuplicateFile
		/// </summary>
		internal enum DuplicateFile
		{
			/// <summary>Index to column name FileKey into Record for row in Msi Table DuplicateFile</summary>
			FileKey = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table DuplicateFile</summary>
			Component = 2,
			/// <summary>Index to column name File into Record for row in Msi Table DuplicateFile</summary>
			File = 3,
			/// <summary>Index to column name DestName into Record for row in Msi Table DuplicateFile</summary>
			DestName = 4,
			/// <summary>Index to column name DestFolder into Record for row in Msi Table DuplicateFile</summary>
			DestFolder = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table Environment
		/// </summary>
		internal enum Environment
		{
			/// <summary>Index to column name Environment into Record for row in Msi Table Environment</summary>
			Environment = 1,
			/// <summary>Index to column name Name into Record for row in Msi Table Environment</summary>
			Name = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table Environment</summary>
			Value = 3,
			/// <summary>Index to column name Component into Record for row in Msi Table Environment</summary>
			Component = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table Error
		/// </summary>
		internal enum Error
		{
			/// <summary>Index to column name Error into Record for row in Msi Table Error</summary>
			Error = 1,
			/// <summary>Index to column name Message into Record for row in Msi Table Error</summary>
			Message = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table EventMapping
		/// </summary>
		internal enum EventMapping
		{
			/// <summary>Index to column name Dialog into Record for row in Msi Table EventMapping</summary>
			Dialog = 1,
			/// <summary>Index to column name Control into Record for row in Msi Table EventMapping</summary>
			Control = 2,
			/// <summary>Index to column name Event into Record for row in Msi Table EventMapping</summary>
			Event = 3,
			/// <summary>Index to column name Attribute into Record for row in Msi Table EventMapping</summary>
			Attribute = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table Extension
		/// </summary>
		internal enum Extension
		{
			/// <summary>Index to column name Extension into Record for row in Msi Table Extension</summary>
			Extension = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table Extension</summary>
			Component = 2,
			/// <summary>Index to column name ProgId into Record for row in Msi Table Extension</summary>
			ProgId = 3,
			/// <summary>Index to column name MIME into Record for row in Msi Table Extension</summary>
			MIME = 4,
			/// <summary>Index to column name Feature into Record for row in Msi Table Extension</summary>
			Feature = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table Feature
		/// </summary>
		internal enum Feature
		{
			/// <summary>Index to column name Feature into Record for row in Msi Table Feature</summary>
			Feature = 1,
			/// <summary>Index to column name FeatureParent into Record for row in Msi Table Feature</summary>
			FeatureParent = 2,
			/// <summary>Index to column name Title into Record for row in Msi Table Feature</summary>
			Title = 3,
			/// <summary>Index to column name Description into Record for row in Msi Table Feature</summary>
			Description = 4,
			/// <summary>Index to column name Display into Record for row in Msi Table Feature</summary>
			Display = 5,
			/// <summary>Index to column name Level into Record for row in Msi Table Feature</summary>
			Level = 6,
			/// <summary>Index to column name Directory into Record for row in Msi Table Feature</summary>
			Directory = 7,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Feature</summary>
			Attributes = 8,
		}

		/// <summary>
		/// Record Indices into Msi Table FeatureComponents
		/// </summary>
		internal enum FeatureComponents
		{
			/// <summary>Index to column name Feature into Record for row in Msi Table FeatureComponents</summary>
			Feature = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table FeatureComponents</summary>
			Component = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table File
		/// </summary>
		internal enum File
		{
			/// <summary>Index to column name File into Record for row in Msi Table File</summary>
			File = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table File</summary>
			Component = 2,
			/// <summary>Index to column name FileName into Record for row in Msi Table File</summary>
			FileName = 3,
			/// <summary>Index to column name FileSize into Record for row in Msi Table File</summary>
			FileSize = 4,
			/// <summary>Index to column name Version into Record for row in Msi Table File</summary>
			Version = 5,
			/// <summary>Index to column name Language into Record for row in Msi Table File</summary>
			Language = 6,
			/// <summary>Index to column name Attributes into Record for row in Msi Table File</summary>
			Attributes = 7,
			/// <summary>Index to column name Sequence into Record for row in Msi Table File</summary>
			Sequence = 8,
		}

		/// <summary>
		/// Record Indices into Msi Table FileSFPCatalog
		/// </summary>
		internal enum FileSFPCatalog
		{
			/// <summary>Index to column name File_ into Record for row in Msi Table FileSFPCatalog</summary>
			File_ = 1,
			/// <summary>Index to column name SFPCatalog_ into Record for row in Msi Table FileSFPCatalog</summary>
			SFPCatalog_ = 2,
		}
		/// <summary>
		/// Record Indices into Msi Table Font
		/// </summary>
		internal enum Font
		{
			/// <summary>Index to column name File into Record for row in Msi Table Font</summary>
			File = 1,
			/// <summary>Index to column name FontTitle into Record for row in Msi Table Font</summary>
			FontTitle = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table Icon
		/// </summary>
		internal enum Icon
		{
			/// <summary>Index to column name Name into Record for row in Msi Table Icon</summary>
			Name = 1,
			/// <summary>Index to column name Data into Record for row in Msi Table Icon</summary>
			Data = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table IniFile
		/// </summary>
		internal enum IniFile
		{
			/// <summary>Index to column name IniFile into Record for row in Msi Table IniFile</summary>
			IniFile = 1,
			/// <summary>Index to column name FileName into Record for row in Msi Table IniFile</summary>
			FileName = 2,
			/// <summary>Index to column name DirProperty into Record for row in Msi Table IniFile</summary>
			DirProperty = 3,
			/// <summary>Index to column name Section into Record for row in Msi Table IniFile</summary>
			Section = 4,
			/// <summary>Index to column name Key into Record for row in Msi Table IniFile</summary>
			Key = 5,
			/// <summary>Index to column name Value into Record for row in Msi Table IniFile</summary>
			Value = 6,
			/// <summary>Index to column name Action into Record for row in Msi Table IniFile</summary>
			Action = 7,
			/// <summary>Index to column name Component into Record for row in Msi Table IniFile</summary>
			Component = 8,
		}

		/// <summary>
		/// Record Indices into Msi Table IniLocator
		/// </summary>
		internal enum IniLocator
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table IniLocator</summary>
			Signature = 1,
			/// <summary>Index to column name FileName into Record for row in Msi Table IniLocator</summary>
			FileName = 2,
			/// <summary>Index to column name Section into Record for row in Msi Table IniLocator</summary>
			Section = 3,
			/// <summary>Index to column name Key into Record for row in Msi Table IniLocator</summary>
			Key = 4,
			/// <summary>Index to column name Field into Record for row in Msi Table IniLocator</summary>
			Field = 5,
			/// <summary>Index to column name Type into Record for row in Msi Table IniLocator</summary>
			Type = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table InstallExecuteSequence
		/// </summary>
		internal enum InstallExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table InstallExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table InstallExecuteSequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table InstallExecuteSequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table InstallUISequence
		/// </summary>
		internal enum InstallUISequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table InstallUISequence</summary>
			Action = 1,
			/// <summary>Index to column name Condition into Record for row in Msi Table InstallUISequence</summary>
			Condition = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table InstallUISequence</summary>
			Sequence = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table IsolatedComponent
		/// </summary>
		internal enum IsolatedComponent
		{
			/// <summary>Index to column name ComponentShared into Record for row in Msi Table IsolatedComponent</summary>
			ComponentShared = 1,
			/// <summary>Index to column name ComponentApplication into Record for row in Msi Table IsolatedComponent</summary>
			ComponentApplication = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table LaunchCondition
		/// </summary>
		internal enum LaunchCondition
		{
			/// <summary>Index to column name Condition into Record for row in Msi Table LaunchCondition</summary>
			Condition = 1,
			/// <summary>Index to column name Description into Record for row in Msi Table LaunchCondition</summary>
			Description = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table ListBox
		/// </summary>
		internal enum ListBox
		{
			/// <summary>Index to column name Property into Record for row in Msi Table ListBox</summary>
			Property = 1,
			/// <summary>Index to column name Order into Record for row in Msi Table ListBox</summary>
			Order = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table ListBox</summary>
			Value = 3,
			/// <summary>Index to column name Text into Record for row in Msi Table ListBox</summary>
			Text = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table ListView
		/// </summary>
		internal enum ListView
		{
			/// <summary>Index to column name Property into Record for row in Msi Table ListView</summary>
			Property = 1,
			/// <summary>Index to column name Order into Record for row in Msi Table ListView</summary>
			Order = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table ListView</summary>
			Value = 3,
			/// <summary>Index to column name Text into Record for row in Msi Table ListView</summary>
			Text = 4,
			/// <summary>Index to column name Binary into Record for row in Msi Table ListView</summary>
			Binary = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table LockPermissions
		/// </summary>
		internal enum LockPermissions
		{
			/// <summary>Index to column name LockObject into Record for row in Msi Table LockPermissions</summary>
			LockObject = 1,
			/// <summary>Index to column name Table into Record for row in Msi Table LockPermissions</summary>
			Table = 2,
			/// <summary>Index to column name Domain into Record for row in Msi Table LockPermissions</summary>
			Domain = 3,
			/// <summary>Index to column name User into Record for row in Msi Table LockPermissions</summary>
			User = 4,
			/// <summary>Index to column name Permission into Record for row in Msi Table LockPermissions</summary>
			Permission = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table MIME
		/// </summary>
		internal enum MIME
		{
			/// <summary>Index to column name ContentType into Record for row in Msi Table MIME</summary>
			ContentType = 1,
			/// <summary>Index to column name Extension into Record for row in Msi Table MIME</summary>
			Extension = 2,
			/// <summary>Index to column name CLSID into Record for row in Msi Table MIME</summary>
			CLSID = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table Media
		/// </summary>
		internal enum Media
		{
			/// <summary>Index to column name DiskId into Record for row in Msi Table Media</summary>
			DiskId = 1,
			/// <summary>Index to column name LastSequence into Record for row in Msi Table Media</summary>
			LastSequence = 2,
			/// <summary>Index to column name DiskPrompt into Record for row in Msi Table Media</summary>
			DiskPrompt = 3,
			/// <summary>Index to column name Cabinet into Record for row in Msi Table Media</summary>
			Cabinet = 4,
			/// <summary>Index to column name VolumeLabel into Record for row in Msi Table Media</summary>
			VolumeLabel = 5,
			/// <summary>Index to column name Source into Record for row in Msi Table Media</summary>
			Source = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleAdminExecuteSequence
		/// </summary>
		internal enum ModuleAdminExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ModuleAdminExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table ModuleAdminExecuteSequence</summary>
			Sequence = 2,
			/// <summary>Index to column name BaseAction into Record for row in Msi Table ModuleAdminExecuteSequence</summary>
			BaseAction = 3,
			/// <summary>Index to column name After into Record for row in Msi Table ModuleAdminExecuteSequence</summary>
			After = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ModuleAdminExecuteSequence</summary>
			Condition = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleAdminUISequence
		/// </summary>
		internal enum ModuleAdminUISequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ModuleAdminUISequence</summary>
			Action = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table ModuleAdminUISequence</summary>
			Sequence = 2,
			/// <summary>Index to column name BaseAction into Record for row in Msi Table ModuleAdminUISequence</summary>
			BaseAction = 3,
			/// <summary>Index to column name After into Record for row in Msi Table ModuleAdminUISequence</summary>
			After = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ModuleAdminUISequence</summary>
			Condition = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleAdvtExecuteSequence
		/// </summary>
		internal enum ModuleAdvtExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ModuleAdvtExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table ModuleAdvtExecuteSequence</summary>
			Sequence = 2,
			/// <summary>Index to column name BaseAction into Record for row in Msi Table ModuleAdvtExecuteSequence</summary>
			BaseAction = 3,
			/// <summary>Index to column name After into Record for row in Msi Table ModuleAdvtExecuteSequence</summary>
			After = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ModuleAdvtExecuteSequence</summary>
			Condition = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleConfiguration
		/// </summary>
		internal enum ModuleConfiguration
		{
			/// <summary>Index to column name Name into Record for row in Msi Table ModuleConfiguration</summary>
			Name = 1,
			/// <summary>Index to column name Format into Record for row in Msi Table ModuleConfiguration</summary>
			Format = 2,
			/// <summary>Index to column name Type into Record for row in Msi Table ModuleConfiguration</summary>
			Type = 3,
			/// <summary>Index to column name ContextData into Record for row in Msi Table ModuleConfiguration</summary>
			ContextData = 4,
			/// <summary>Index to column name DefaultValue into Record for row in Msi Table ModuleConfiguration</summary>
			DefaultValue = 5,
			/// <summary>Index to column name Attributes into Record for row in Msi Table ModuleConfiguration</summary>
			Attributes = 6,
			/// <summary>Index to column name DisplayName into Record for row in Msi Table ModuleConfiguration</summary>
			DisplayName = 7,
			/// <summary>Index to column name Description into Record for row in Msi Table ModuleConfiguration</summary>
			Description = 8,
			/// <summary>Index to column name HelpLocation into Record for row in Msi Table ModuleConfiguration</summary>
			HelpLocation = 9,
			/// <summary>Index to column name HelpKeyword into Record for row in Msi Table ModuleConfiguration</summary>
			HelpKeyword = 10,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleDependency
		/// </summary>
		internal enum ModuleDependency
		{
			/// <summary>Index to column name ModuleID into Record for row in Msi Table ModuleDependency</summary>
			ModuleID = 1,
			/// <summary>Index to column name ModuleLanguage into Record for row in Msi Table ModuleDependency</summary>
			ModuleLanguage = 2,
			/// <summary>Index to column name RequiredID into Record for row in Msi Table ModuleDependency</summary>
			RequiredID = 3,
			/// <summary>Index to column name RequiredLanguage into Record for row in Msi Table ModuleDependency</summary>
			RequiredLanguage = 4,
			/// <summary>Index to column name RequiredVersion into Record for row in Msi Table ModuleDependency</summary>
			RequiredVersion = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleExclusion
		/// </summary>
		internal enum ModuleExclusion
		{
			/// <summary>Index to column name ModuleID into Record for row in Msi Table ModuleExclusion</summary>
			ModuleID = 1,
			/// <summary>Index to column name ModuleLanguage into Record for row in Msi Table ModuleExclusion</summary>
			ModuleLanguage = 2,
			/// <summary>Index to column name ExcludedID into Record for row in Msi Table ModuleExclusion</summary>
			ExcludedID = 3,
			/// <summary>Index to column name ExcludedLanguage into Record for row in Msi Table ModuleExclusion</summary>
			ExcludedLanguage = 4,
			/// <summary>Index to column name ExcludedMinVersion into Record for row in Msi Table ModuleExclusion</summary>
			ExcludedMinVersion = 5,
			/// <summary>Index to column name ExcludedMaxVersion into Record for row in Msi Table ModuleExclusion</summary>
			ExcludedMaxVersion = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleIgnoreTable
		/// </summary>
		internal enum ModuleIgnoreTable
		{
			/// <summary>Index to column name Table into Record for row in Msi Table ModuleIgnoreTable</summary>
			Table = 1,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleInstallExecuteSequence
		/// </summary>
		internal enum ModuleInstallExecuteSequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ModuleInstallExecuteSequence</summary>
			Action = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table ModuleInstallExecuteSequence</summary>
			Sequence = 2,
			/// <summary>Index to column name BaseAction into Record for row in Msi Table ModuleInstallExecuteSequence</summary>
			BaseAction = 3,
			/// <summary>Index to column name After into Record for row in Msi Table ModuleInstallExecuteSequence</summary>
			After = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ModuleInstallExecuteSequence</summary>
			Condition = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleInstallUISequence
		/// </summary>
		internal enum ModuleInstallUISequence
		{
			/// <summary>Index to column name Action into Record for row in Msi Table ModuleInstallUISequence</summary>
			Action = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table ModuleInstallUISequence</summary>
			Sequence = 2,
			/// <summary>Index to column name BaseAction into Record for row in Msi Table ModuleInstallUISequence</summary>
			BaseAction = 3,
			/// <summary>Index to column name After into Record for row in Msi Table ModuleInstallUISequence</summary>
			After = 4,
			/// <summary>Index to column name Condition into Record for row in Msi Table ModuleInstallUISequence</summary>
			Condition = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleSignature
		/// </summary>
		internal enum ModuleSignature
		{
			/// <summary>Index to column name ModuleID into Record for row in Msi Table ModuleSignature</summary>
			ModuleID = 1,
			/// <summary>Index to column name Language into Record for row in Msi Table ModuleSignature</summary>
			Language = 2,
			/// <summary>Index to column name Version into Record for row in Msi Table ModuleSignature</summary>
			Version = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table ModuleSubstitution
		/// </summary>
		internal enum ModuleSubstitution
		{
			/// <summary>Index to column name Table into Record for row in Msi Table ModuleSubstitution</summary>
			Table = 1,
			/// <summary>Index to column name Row into Record for row in Msi Table ModuleSubstitution</summary>
			Row = 2,
			/// <summary>Index to column name Column into Record for row in Msi Table ModuleSubstitution</summary>
			Column = 3,
			/// <summary>Index to column name Value into Record for row in Msi Table ModuleSubstitution</summary>
			Value = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table MoveFile
		/// </summary>
		internal enum MoveFile
		{
			/// <summary>Index to column name FileKey into Record for row in Msi Table MoveFile</summary>
			FileKey = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table MoveFile</summary>
			Component = 2,
			/// <summary>Index to column name SourceName into Record for row in Msi Table MoveFile</summary>
			SourceName = 3,
			/// <summary>Index to column name DestName into Record for row in Msi Table MoveFile</summary>
			DestName = 4,
			/// <summary>Index to column name SourceFolder into Record for row in Msi Table MoveFile</summary>
			SourceFolder = 5,
			/// <summary>Index to column name DestFolder into Record for row in Msi Table MoveFile</summary>
			DestFolder = 6,
			/// <summary>Index to column name Options into Record for row in Msi Table MoveFile</summary>
			Options = 7,
		}

		/// <summary>
		/// Record Indices into Msi Table MsiAssembly
		/// </summary>
		internal enum MsiAssembly
		{
			/// <summary>Index to column name Component into Record for row in Msi Table MsiAssembly</summary>
			Component = 1,
			/// <summary>Index to column name Feature into Record for row in Msi Table MsiAssembly</summary>
			Feature = 2,
			/// <summary>Index to column name FileManifest into Record for row in Msi Table MsiAssembly</summary>
			FileManifest = 3,
			/// <summary>Index to column name FileApplication into Record for row in Msi Table MsiAssembly</summary>
			FileApplication = 4,
			/// <summary>Index to column name Attributes into Record for row in Msi Table MsiAssembly</summary>
			Attributes = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table MsiAssemblyName
		/// </summary>
		internal enum MsiAssemblyName
		{
			/// <summary>Index to column name Component into Record for row in Msi Table MsiAssemblyName</summary>
			Component = 1,
			/// <summary>Index to column name Name into Record for row in Msi Table MsiAssemblyName</summary>
			Name = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table MsiAssemblyName</summary>
			Value = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table MsiDigitalCertificate
		/// </summary>
		internal enum MsiDigitalCertificate
		{
			/// <summary>Index to column name DigitalCertificate into Record for row in Msi Table MsiDigitalCertificate</summary>
			DigitalCertificate = 1,
			/// <summary>Index to column name CertData into Record for row in Msi Table MsiDigitalCertificate</summary>
			CertData = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table MsiDigitalSignature
		/// </summary>
		internal enum MsiDigitalSignature
		{
			/// <summary>Index to column name Table into Record for row in Msi Table MsiDigitalSignature</summary>
			Table = 1,
			/// <summary>Index to column name SignObject into Record for row in Msi Table MsiDigitalSignature</summary>
			SignObject = 2,
			/// <summary>Index to column name DigitalCertificate into Record for row in Msi Table MsiDigitalSignature</summary>
			DigitalCertificate = 3,
			/// <summary>Index to column name Hash into Record for row in Msi Table MsiDigitalSignature</summary>
			Hash = 4,
		}

		/// <summary>
		/// Record Indices into Msi Table MsiPatchCertificate
		/// </summary>
		internal enum MsiPatchCertificate
		{
			/// <summary>Index to column name PatchCertificate into Record for row in Msi Table MsiPatchCertificate</summary>
			PatchCertificate = 1,

			/// <summary>Index to column name DigitalCertificate into Record for row in Msi Table MsiPatchCertificate</summary>
			DigitalCertificate = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table ODBCAttribute
		/// </summary>
		internal enum ODBCAttribute
		{
			/// <summary>Index to column name Driver into Record for row in Msi Table ODBCAttribute</summary>
			Driver = 1,
			/// <summary>Index to column name Attribute into Record for row in Msi Table ODBCAttribute</summary>
			Attribute = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table ODBCAttribute</summary>
			Value = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table ODBCDataSource
		/// </summary>
		internal enum ODBCDataSource
		{
			/// <summary>Index to column name DataSource into Record for row in Msi Table ODBCDataSource</summary>
			DataSource = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table ODBCDataSource</summary>
			Component = 2,
			/// <summary>Index to column name Description into Record for row in Msi Table ODBCDataSource</summary>
			Description = 3,
			/// <summary>Index to column name DriverDescription into Record for row in Msi Table ODBCDataSource</summary>
			DriverDescription = 4,
			/// <summary>Index to column name Registration into Record for row in Msi Table ODBCDataSource</summary>
			Registration = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ODBCDriver
		/// </summary>
		internal enum ODBCDriver
		{
			/// <summary>Index to column name Driver into Record for row in Msi Table ODBCDriver</summary>
			Driver = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table ODBCDriver</summary>
			Component = 2,
			/// <summary>Index to column name Description into Record for row in Msi Table ODBCDriver</summary>
			Description = 3,
			/// <summary>Index to column name File into Record for row in Msi Table ODBCDriver</summary>
			File = 4,
			/// <summary>Index to column name FileSetup into Record for row in Msi Table ODBCDriver</summary>
			FileSetup = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ODBCSourceAttribute
		/// </summary>
		internal enum ODBCSourceAttribute
		{
			/// <summary>Index to column name DataSource into Record for row in Msi Table ODBCSourceAttribute</summary>
			DataSource = 1,
			/// <summary>Index to column name Attribute into Record for row in Msi Table ODBCSourceAttribute</summary>
			Attribute = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table ODBCSourceAttribute</summary>
			Value = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table ODBCTranslator
		/// </summary>
		internal enum ODBCTranslator
		{
			/// <summary>Index to column name Translator into Record for row in Msi Table ODBCTranslator</summary>
			Translator = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table ODBCTranslator</summary>
			Component = 2,
			/// <summary>Index to column name Description into Record for row in Msi Table ODBCTranslator</summary>
			Description = 3,
			/// <summary>Index to column name File into Record for row in Msi Table ODBCTranslator</summary>
			File = 4,
			/// <summary>Index to column name FileSetup into Record for row in Msi Table ODBCTranslator</summary>
			FileSetup = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table Patch
		/// </summary>
		internal enum Patch
		{
			/// <summary>Index to column name File into Record for row in Msi Table Patch</summary>
			File = 1,
			/// <summary>Index to column name Sequence into Record for row in Msi Table Patch</summary>
			Sequence = 2,
			/// <summary>Index to column name PatchSize into Record for row in Msi Table Patch</summary>
			PatchSize = 3,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Patch</summary>
			Attributes = 4,
			/// <summary>Index to column name Header into Record for row in Msi Table Patch</summary>
			Header = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table PatchPackage
		/// </summary>
		internal enum PatchPackage
		{
			/// <summary>Index to column name PatchId into Record for row in Msi Table PatchPackage</summary>
			PatchId = 1,
			/// <summary>Index to column name Media into Record for row in Msi Table PatchPackage</summary>
			Media = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table ProgId
		/// </summary>
		internal enum ProgId
		{
			/// <summary>Index to column name ProgId into Record for row in Msi Table ProgId</summary>
			ProgId = 1,
			/// <summary>Index to column name ProgIdParent into Record for row in Msi Table ProgId</summary>
			ProgIdParent = 2,
			/// <summary>Index to column name Class into Record for row in Msi Table ProgId</summary>
			Class = 3,
			/// <summary>Index to column name Description into Record for row in Msi Table ProgId</summary>
			Description = 4,
			/// <summary>Index to column name Icon into Record for row in Msi Table ProgId</summary>
			Icon = 5,
			/// <summary>Index to column name IconIndex into Record for row in Msi Table ProgId</summary>
			IconIndex = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table Property
		/// </summary>
		internal enum Property
		{
			/// <summary>Index to column name Property into Record for row in Msi Table Property</summary>
			Property = 1,
			/// <summary>Index to column name Value into Record for row in Msi Table Property</summary>
			Value = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table PublishComponent
		/// </summary>
		internal enum PublishComponent
		{
			/// <summary>Index to column name ComponentId into Record for row in Msi Table PublishComponent</summary>
			ComponentId = 1,
			/// <summary>Index to column name Qualifier into Record for row in Msi Table PublishComponent</summary>
			Qualifier = 2,
			/// <summary>Index to column name Component into Record for row in Msi Table PublishComponent</summary>
			Component = 3,
			/// <summary>Index to column name AppData into Record for row in Msi Table PublishComponent</summary>
			AppData = 4,
			/// <summary>Index to column name Feature into Record for row in Msi Table PublishComponent</summary>
			Feature = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table RadioButton
		/// </summary>
		internal enum RadioButton
		{
			/// <summary>Index to column name Property into Record for row in Msi Table RadioButton</summary>
			Property = 1,
			/// <summary>Index to column name Order into Record for row in Msi Table RadioButton</summary>
			Order = 2,
			/// <summary>Index to column name Value into Record for row in Msi Table RadioButton</summary>
			Value = 3,
			/// <summary>Index to column name X into Record for row in Msi Table RadioButton</summary>
			X = 4,
			/// <summary>Index to column name Y into Record for row in Msi Table RadioButton</summary>
			Y = 5,
			/// <summary>Index to column name Width into Record for row in Msi Table RadioButton</summary>
			Width = 6,
			/// <summary>Index to column name Height into Record for row in Msi Table RadioButton</summary>
			Height = 7,
			/// <summary>Index to column name Text into Record for row in Msi Table RadioButton</summary>
			Text = 8,
			/// <summary>Index to column name Help into Record for row in Msi Table RadioButton</summary>
			Help = 9,
		}

		/// <summary>
		/// Record Indices into Msi Table RegLocator
		/// </summary>
		internal enum RegLocator
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table RegLocator</summary>
			Signature = 1,
			/// <summary>Index to column name Root into Record for row in Msi Table RegLocator</summary>
			Root = 2,
			/// <summary>Index to column name Key into Record for row in Msi Table RegLocator</summary>
			Key = 3,
			/// <summary>Index to column name Name into Record for row in Msi Table RegLocator</summary>
			Name = 4,
			/// <summary>Index to column name Type into Record for row in Msi Table RegLocator</summary>
			Type = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table Registry
		/// </summary>
		internal enum Registry
		{
			/// <summary>Index to column name Registry into Record for row in Msi Table Registry</summary>
			Registry = 1,
			/// <summary>Index to column name Root into Record for row in Msi Table Registry</summary>
			Root = 2,
			/// <summary>Index to column name Key into Record for row in Msi Table Registry</summary>
			Key = 3,
			/// <summary>Index to column name Name into Record for row in Msi Table Registry</summary>
			Name = 4,
			/// <summary>Index to column name Value into Record for row in Msi Table Registry</summary>
			Value = 5,
			/// <summary>Index to column name Component into Record for row in Msi Table Registry</summary>
			Component = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table RemoveFile
		/// </summary>
		internal enum RemoveFile
		{
			/// <summary>Index to column name FileKey into Record for row in Msi Table RemoveFile</summary>
			FileKey = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table RemoveFile</summary>
			Component = 2,
			/// <summary>Index to column name FileName into Record for row in Msi Table RemoveFile</summary>
			FileName = 3,
			/// <summary>Index to column name DirProperty into Record for row in Msi Table RemoveFile</summary>
			DirProperty = 4,
			/// <summary>Index to column name InstallMode into Record for row in Msi Table RemoveFile</summary>
			InstallMode = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table RemoveIniFile
		/// </summary>
		internal enum RemoveIniFile
		{
			/// <summary>Index to column name RemoveIniFile into Record for row in Msi Table RemoveIniFile</summary>
			RemoveIniFile = 1,
			/// <summary>Index to column name FileName into Record for row in Msi Table RemoveIniFile</summary>
			FileName = 2,
			/// <summary>Index to column name DirProperty into Record for row in Msi Table RemoveIniFile</summary>
			DirProperty = 3,
			/// <summary>Index to column name Section into Record for row in Msi Table RemoveIniFile</summary>
			Section = 4,
			/// <summary>Index to column name Key into Record for row in Msi Table RemoveIniFile</summary>
			Key = 5,
			/// <summary>Index to column name Value into Record for row in Msi Table RemoveIniFile</summary>
			Value = 6,
			/// <summary>Index to column name Action into Record for row in Msi Table RemoveIniFile</summary>
			Action = 7,
			/// <summary>Index to column name Component into Record for row in Msi Table RemoveIniFile</summary>
			Component = 8,
		}

		/// <summary>
		/// Record Indices into Msi Table RemoveRegistry
		/// </summary>
		internal enum RemoveRegistry
		{
			/// <summary>Index to column name RemoveRegistry into Record for row in Msi Table RemoveRegistry</summary>
			RemoveRegistry = 1,
			/// <summary>Index to column name Root into Record for row in Msi Table RemoveRegistry</summary>
			Root = 2,
			/// <summary>Index to column name Key into Record for row in Msi Table RemoveRegistry</summary>
			Key = 3,
			/// <summary>Index to column name Name into Record for row in Msi Table RemoveRegistry</summary>
			Name = 4,
			/// <summary>Index to column name Component into Record for row in Msi Table RemoveRegistry</summary>
			Component = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table ReserveCost
		/// </summary>
		internal enum ReserveCost
		{
			/// <summary>Index to column name ReserveKey into Record for row in Msi Table ReserveCost</summary>
			ReserveKey = 1,
			/// <summary>Index to column name Component into Record for row in Msi Table ReserveCost</summary>
			Component = 2,
			/// <summary>Index to column name ReserveFolder into Record for row in Msi Table ReserveCost</summary>
			ReserveFolder = 3,
			/// <summary>Index to column name ReserveLocal into Record for row in Msi Table ReserveCost</summary>
			ReserveLocal = 4,
			/// <summary>Index to column name ReserveSource into Record for row in Msi Table ReserveCost</summary>
			ReserveSource = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table SelfReg
		/// </summary>
		internal enum SelfReg
		{
			/// <summary>Index to column name File into Record for row in Msi Table SelfReg</summary>
			File = 1,
			/// <summary>Index to column name Cost into Record for row in Msi Table SelfReg</summary>
			Cost = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table ServiceControl
		/// </summary>
		internal enum ServiceControl
		{
			/// <summary>Index to column name ServiceControl into Record for row in Msi Table ServiceControl</summary>
			ServiceControl = 1,
			/// <summary>Index to column name Name into Record for row in Msi Table ServiceControl</summary>
			Name = 2,
			/// <summary>Index to column name Event into Record for row in Msi Table ServiceControl</summary>
			Event = 3,
			/// <summary>Index to column name Arguments into Record for row in Msi Table ServiceControl</summary>
			Arguments = 4,
			/// <summary>Index to column name Wait into Record for row in Msi Table ServiceControl</summary>
			Wait = 5,
			/// <summary>Index to column name Component into Record for row in Msi Table ServiceControl</summary>
			Component = 6,
		}

		/// <summary>
		/// Record Indices into Msi Table ServiceInstall
		/// </summary>
		internal enum ServiceInstall
		{
			/// <summary>Index to column name ServiceInstall into Record for row in Msi Table ServiceInstall</summary>
			ServiceInstall = 1,
			/// <summary>Index to column name Name into Record for row in Msi Table ServiceInstall</summary>
			Name = 2,
			/// <summary>Index to column name DisplayName into Record for row in Msi Table ServiceInstall</summary>
			DisplayName = 3,
			/// <summary>Index to column name ServiceType into Record for row in Msi Table ServiceInstall</summary>
			ServiceType = 4,
			/// <summary>Index to column name StartType into Record for row in Msi Table ServiceInstall</summary>
			StartType = 5,
			/// <summary>Index to column name ErrorControl into Record for row in Msi Table ServiceInstall</summary>
			ErrorControl = 6,
			/// <summary>Index to column name LoadOrderGroup into Record for row in Msi Table ServiceInstall</summary>
			LoadOrderGroup = 7,
			/// <summary>Index to column name Dependencies into Record for row in Msi Table ServiceInstall</summary>
			Dependencies = 8,
			/// <summary>Index to column name StartName into Record for row in Msi Table ServiceInstall</summary>
			StartName = 9,
			/// <summary>Index to column name Password into Record for row in Msi Table ServiceInstall</summary>
			Password = 10,
			/// <summary>Index to column name Arguments into Record for row in Msi Table ServiceInstall</summary>
			Arguments = 11,
			/// <summary>Index to column name Component into Record for row in Msi Table ServiceInstall</summary>
			Component = 12,
			/// <summary>Index to column name Description into Record for row in Msi Table ServiceInstall</summary>
			Description = 13,
		}

		/// <summary>
		/// Record Indices into Msi Table SFPCatalog
		/// </summary>
		internal enum SFPCatalog
		{
			/// <summary>Index to column name SFPCatalog into Record for row in Msi Table SFPCatalog</summary>
			SFPCatalog = 1,
			/// <summary>Index to column name Catalog into Record for row in Msi Table SFPCatalog</summary>
			Catalog = 2,
			/// <summary>Index to column name Dependency into Record for row in Msi Table SFPCatalog</summary>
			Dependency = 3,
		}

		/// <summary>
		/// Record Indices into Msi Table Shortcut
		/// </summary>
		internal enum Shortcut
		{
			/// <summary>Index to column name Shortcut into Record for row in Msi Table Shortcut</summary>
			Shortcut = 1,
			/// <summary>Index to column name Directory into Record for row in Msi Table Shortcut</summary>
			Directory = 2,
			/// <summary>Index to column name Name into Record for row in Msi Table Shortcut</summary>
			Name = 3,
			/// <summary>Index to column name Component into Record for row in Msi Table Shortcut</summary>
			Component = 4,
			/// <summary>Index to column name Target into Record for row in Msi Table Shortcut</summary>
			Target = 5,
			/// <summary>Index to column name Arguments into Record for row in Msi Table Shortcut</summary>
			Arguments = 6,
			/// <summary>Index to column name Description into Record for row in Msi Table Shortcut</summary>
			Description = 7,
			/// <summary>Index to column name Hotkey into Record for row in Msi Table Shortcut</summary>
			Hotkey = 8,
			/// <summary>Index to column name Icon into Record for row in Msi Table Shortcut</summary>
			Icon = 9,
			/// <summary>Index to column name IconIndex into Record for row in Msi Table Shortcut</summary>
			IconIndex = 10,
			/// <summary>Index to column name ShowCmd into Record for row in Msi Table Shortcut</summary>
			ShowCmd = 11,
			/// <summary>Index to column name WkDir into Record for row in Msi Table Shortcut</summary>
			WkDir = 12,
			/// <summary>Index to column name DisplayResourceDLL into Record for row in Msi Table Shortcut</summary>
			DisplayResourceDLL = 13,
			/// <summary>Index to column name DisplayResourceId into Record for row in Msi Table Shortcut</summary>
			DisplayResourceId = 14,
			/// <summary>Index to column name DescriptionResourceDLL into Record for row in Msi Table Shortcut</summary>
			DescriptionResourceDLL = 15,
			/// <summary>Index to column name DescriptionResourceId into Record for row in Msi Table Shortcut</summary>
			DescriptionResourceId = 16,
		}

		/// <summary>
		/// Record Indices into Msi Table Signature
		/// </summary>
		internal enum Signature
		{
			/// <summary>Index to column name Signature into Record for row in Msi Table Signature</summary>
			Signature = 1,
			/// <summary>Index to column name FileName into Record for row in Msi Table Signature</summary>
			FileName = 2,
			/// <summary>Index to column name MinVersion into Record for row in Msi Table Signature</summary>
			MinVersion = 3,
			/// <summary>Index to column name MaxVersion into Record for row in Msi Table Signature</summary>
			MaxVersion = 4,
			/// <summary>Index to column name MinSize into Record for row in Msi Table Signature</summary>
			MinSize = 5,
			/// <summary>Index to column name MaxSize into Record for row in Msi Table Signature</summary>
			MaxSize = 6,
			/// <summary>Index to column name MinDate into Record for row in Msi Table Signature</summary>
			MinDate = 7,
			/// <summary>Index to column name MaxDate into Record for row in Msi Table Signature</summary>
			MaxDate = 8,
			/// <summary>Index to column name Languages into Record for row in Msi Table Signature</summary>
			Languages = 9,
		}

		/// <summary>
		/// Record Indices into Msi Table TextStyle
		/// </summary>
		internal enum TextStyle
		{
			/// <summary>Index to column name TextStyle into Record for row in Msi Table TextStyle</summary>
			TextStyle = 1,
			/// <summary>Index to column name FaceName into Record for row in Msi Table TextStyle</summary>
			FaceName = 2,
			/// <summary>Index to column name Size into Record for row in Msi Table TextStyle</summary>
			Size = 3,
			/// <summary>Index to column name Color into Record for row in Msi Table TextStyle</summary>
			Color = 4,
			/// <summary>Index to column name StyleBits into Record for row in Msi Table TextStyle</summary>
			StyleBits = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table TypeLib
		/// </summary>
		internal enum TypeLib
		{
			/// <summary>Index to column name LibID into Record for row in Msi Table TypeLib</summary>
			LibID = 1,
			/// <summary>Index to column name Language into Record for row in Msi Table TypeLib</summary>
			Language = 2,
			/// <summary>Index to column name Component into Record for row in Msi Table TypeLib</summary>
			Component = 3,
			/// <summary>Index to column name Version into Record for row in Msi Table TypeLib</summary>
			Version = 4,
			/// <summary>Index to column name Description into Record for row in Msi Table TypeLib</summary>
			Description = 5,
			/// <summary>Index to column name Directory into Record for row in Msi Table TypeLib</summary>
			Directory = 6,
			/// <summary>Index to column name Feature into Record for row in Msi Table TypeLib</summary>
			Feature = 7,
			/// <summary>Index to column name Cost into Record for row in Msi Table TypeLib</summary>
			Cost = 8,
		}

		/// <summary>
		/// Record Indices into Msi Table UIText
		/// </summary>
		internal enum UIText
		{
			/// <summary>Index to column name Key into Record for row in Msi Table UIText</summary>
			Key = 1,
			/// <summary>Index to column name Text into Record for row in Msi Table UIText</summary>
			Text = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table Upgrade
		/// </summary>
		internal enum Upgrade
		{
			/// <summary>Index to column name UpgradeCode into Record for row in Msi Table Upgrade</summary>
			UpgradeCode = 1,
			/// <summary>Index to column name VersionMin into Record for row in Msi Table Upgrade</summary>
			VersionMin = 2,
			/// <summary>Index to column name VersionMax into Record for row in Msi Table Upgrade</summary>
			VersionMax = 3,
			/// <summary>Index to column name Language into Record for row in Msi Table Upgrade</summary>
			Language = 4,
			/// <summary>Index to column name Attributes into Record for row in Msi Table Upgrade</summary>
			Attributes = 5,
			/// <summary>Index to column name Remove into Record for row in Msi Table Upgrade</summary>
			Remove = 6,
			/// <summary>Index to column name ActionProperty into Record for row in Msi Table Upgrade</summary>
			ActionProperty = 7,
		}

		/// <summary>
		/// Record Indices into Msi Table Verb
		/// </summary>
		internal enum Verb
		{
			/// <summary>Index to column name Extension into Record for row in Msi Table Verb</summary>
			Extension = 1,
			/// <summary>Index to column name Verb into Record for row in Msi Table Verb</summary>
			Verb = 2,
			/// <summary>Index to column name Sequence into Record for row in Msi Table Verb</summary>
			Sequence = 3,
			/// <summary>Index to column name Command into Record for row in Msi Table Verb</summary>
			Command = 4,
			/// <summary>Index to column name Argument into Record for row in Msi Table Verb</summary>
			Argument = 5,
		}

		/// <summary>
		/// Record Indices into Msi Table Validation
		/// </summary>
		internal enum Validation
		{
			/// <summary>Index to column name Table into Record for row in Msi Table Validation</summary>
			Table = 1,
			/// <summary>Index to column name Column into Record for row in Msi Table Validation</summary>
			Column = 2,
			/// <summary>Index to column name Nullable into Record for row in Msi Table Validation</summary>
			Nullable = 3,
			/// <summary>Index to column name MinValue into Record for row in Msi Table Validation</summary>
			MinValue = 4,
			/// <summary>Index to column name MaxValue into Record for row in Msi Table Validation</summary>
			MaxValue = 5,
			/// <summary>Index to column name KeyTable into Record for row in Msi Table Validation</summary>
			KeyTable = 6,
			/// <summary>Index to column name KeyColumn into Record for row in Msi Table Validation</summary>
			KeyColumn = 7,
			/// <summary>Index to column name Category into Record for row in Msi Table Validation</summary>
			Category = 8,
			/// <summary>Index to column name Set into Record for row in Msi Table Validation</summary>
			Set = 9,
			/// <summary>Index to column name Description into Record for row in Msi Table Validation</summary>
			Description = 10,
		}

		/// <summary>
		/// Record Indices into Msi Table Storages
		/// </summary>
		internal enum Storages
		{
			/// <summary>Index to column name Name into Record for row in Msi Table Storages</summary>
			Name = 1,
			/// <summary>Index to column name Data into Record for row in Msi Table Storages</summary>
			Data = 2,
		}

		/// <summary>
		/// Record Indices into Msi Table Streams
		/// </summary>
		internal enum Streams
		{
			/// <summary>Index to column name Name into Record for row in Msi Table Streams</summary>
			Name = 1,
			/// <summary>Index to column name Data into Record for row in Msi Table Streams</summary>
			Data = 2,
		}

		/// <summary>
		/// PInvoke of MsiCloseHandle.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiCloseHandle", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiCloseHandle(IntPtr database);

		/// <summary>
		/// PInvoke of MsiCreateRecord
		/// </summary>
		/// <param name="parameters">Count of columns in the record.</param>
		/// <returns>IntPtr referencing the record.</returns>
		[DllImport("msi.dll", EntryPoint="MsiCreateRecord", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern IntPtr MsiCreateRecord(int parameters);

		/// <summary>
		/// PInvoke of MsiDatabaseCommit.
		/// </summary>
		/// <param name="database">Handle to a databse.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseCommit", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiDatabaseCommit(IntPtr database);

		/// <summary>
		/// PInvoke of MsiDatabaseExportW.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="tableName">Table name.</param>
		/// <param name="folderPath">Folder path.</param>
		/// <param name="fileName">File name.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseExportW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiDatabaseExport(IntPtr database, string tableName, string folderPath, string fileName);

		/// <summary>
		/// PInvoke of MsiDatabaseImportW.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="folderPath">Folder path.</param>
		/// <param name="fileName">File name.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseImportW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiDatabaseImport(IntPtr database, string folderPath, string fileName);

		/// <summary>
		/// PInvoke of MsiDatabaseOpenViewW.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="query">SQL query.</param>
		/// <param name="view">View handle.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseOpenViewW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiDatabaseOpenView(IntPtr database, string query, out IntPtr view);

		/// <summary>
		/// PInvoke of MsiGetFileHashW.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <param name="options">Hash options (must be 0).</param>
		/// <param name="hash">Buffer to recieve hash.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiGetFileHashW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiGetFileHash(string filePath, uint options, ref MSIFILEHASHINFO hash);

		/// <summary>
		/// PInvoke of MsiGetFileVersionW.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <param name="versionBuf">Buffer to receive version info.</param>
		/// <param name="versionBufSize">Size of version buffer.</param>
		/// <param name="langBuf">Buffer to recieve lang info.</param>
		/// <param name="langBufSize">Size of lang buffer.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiGetFileVersionW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiGetFileVersion(string filePath, StringBuilder versionBuf, ref int versionBufSize, StringBuilder langBuf, ref int langBufSize);

		/// <summary>
		/// PInvoke of MsiDatabaseGetPrimaryKeysW.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="tableName">Table name.</param>
		/// <param name="record">Handle to receive resulting record.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseGetPrimaryKeysW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiDatabaseGetPrimaryKeys(IntPtr database, string tableName, out IntPtr record);

		/// <summary>
		/// PInvoke of MsiGetSummaryInformationW.  Can use either database handle or database path as input.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="databasePath">Path to a database.</param>
		/// <param name="updateCount">Max number of updated values.</param>
		/// <param name="summaryInfo">Handle to summary information.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiGetSummaryInformationW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiGetSummaryInformation(IntPtr database, string databasePath, uint updateCount, ref IntPtr summaryInfo);

		/// <summary>
		/// PInvoke of MsiDatabaseIsTablePersitentW.
		/// </summary>
		/// <param name="database">Handle to a database.</param>
		/// <param name="tableName">Table name.</param>
		/// <returns>MSICONDITION</returns>
		[DllImport("msi.dll", EntryPoint="MsiDatabaseIsTablePersistentW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int MsiDatabaseIsTablePersistent(IntPtr database, string tableName);

		/// <summary>
		/// PInvoke of MSIOpenDatabaseW.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="persist">Persist mode.</param>
		/// <param name="database">Handle to database.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiOpenDatabaseW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiOpenDatabase(string databasePath, string persist, out IntPtr database);

		/// <summary>
		/// PInvoke of MsiOpenDatabaseW.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="persist">Persist mode.</param>
		/// <param name="database">Handle to database.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiOpenDatabaseW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiOpenDatabase(string databasePath, int persist, out IntPtr database);

		/// <summary>
		/// PInvoke of MsiRecordIsNull.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to check for null value.</param>
		/// <returns>true if the field is null, false if not, and an error code for any error.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordIsNull", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordIsNull(IntPtr record, int field);

		/// <summary>
		/// PInvoke of MsiRecordGetInteger.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to retrieve integer from.</param>
		/// <returns>Integer value.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordGetInteger", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int MsiRecordGetInteger(IntPtr record, int field);

		/// <summary>
		/// PInvoke of MsiRectordSetInteger.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to set integer value in.</param>
		/// <param name="value">Value to set field to.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordSetInteger", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordSetInteger(IntPtr record, int field, int value);

		/// <summary>
		/// PInvoke of MsiRecordGetStringW.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to get string value from.</param>
		/// <param name="valueBuf">Buffer to recieve value.</param>
		/// <param name="valueBufSize">Size of buffer.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordGetStringW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordGetString(IntPtr record, int field, StringBuilder valueBuf, ref int valueBufSize);

		/// <summary>
		/// PInvoke of MsiRecordSetStringW.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to set string value in.</param>
		/// <param name="value">String value.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordSetStringW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordSetString(IntPtr record, int field, string value);

		/// <summary>
		/// PInvoke of MsiRecordSetStreamW.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to set stream value in.</param>
		/// <param name="filePath">Path to file to set stream value to.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordSetStreamW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordSetStream(IntPtr record, int field, string filePath);

		/// <summary>
		/// PInvoke of MsiRecordReadStreamW.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <param name="field">Index of field to read stream from.</param>
		/// <param name="dataBuf">Data buffer to recieve stream value.</param>
		/// <param name="dataBufSize">Size of data buffer.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordReadStream", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiRecordReadStream(IntPtr record, int field, byte[] dataBuf, ref int dataBufSize);

		/// <summary>
		/// PInvoke of MsiRecordGetFieldCount.
		/// </summary>
		/// <param name="record">MSI Record handle.</param>
		/// <returns>Count of fields in the record.</returns>
		[DllImport("msi.dll", EntryPoint="MsiRecordGetFieldCount", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern int MsiRecordGetFieldCount(IntPtr record);

		/// <summary>
		/// PInvoke of MsiSummaryInfoPersist.
		/// </summary>
		/// <param name="summaryInfo">Handle to summary info.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiSummaryInfoPersist", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiSummaryInfoPersist(IntPtr summaryInfo);

		/// <summary>
		/// PInvoke of MsiSummaryInfoSetPropertyW.  Must use one of integerValue, fileTimeValue, or stringValue.
		/// </summary>
		/// <param name="summaryInfo">Handle to summary info.</param>
		/// <param name="property">Property to set.</param>
		/// <param name="dataType">Data type of property.</param>
		/// <param name="integerValue">Integer value to set property to.</param>
		/// <param name="fileTimeValue">File time value to set property to.</param>
		/// <param name="stringValue">String value to set property to.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiSummaryInfoSetPropertyW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiSummaryInfoSetProperty(IntPtr summaryInfo, int property, uint dataType, int integerValue, ref FILETIME fileTimeValue, string stringValue);

		/// <summary>
		/// PInvoke of MsiSummaryInfoGetPropertyW.
		/// </summary>
		/// <param name="summaryInfo">Handle to summary info.</param>
		/// <param name="property">Property to get value from.</param>
		/// <param name="dataType">Data type of property.</param>
		/// <param name="integerValue">Integer to receive integer value.</param>
		/// <param name="fileTimeValue">File time to receive file time value.</param>
		/// <param name="stringValueBuf">String buffer to receive string value.</param>
		/// <param name="stringValueBufSize">Size of string buffer.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiSummaryInfoGetPropertyW", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiSummaryInfoGetProperty(IntPtr summaryInfo, int property, out uint dataType, out int integerValue, ref FILETIME fileTimeValue, StringBuilder stringValueBuf, ref int stringValueBufSize);

		/// <summary>
		/// PInvoke of MsiSummaryInfoGetPropertyCount.
		/// </summary>
		/// <param name="summaryInfo">Summary info handle.</param>
		/// <param name="propertyCount">Integer to receive count of properties in the summary info.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiSummaryInfoGetPropertyCount", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiSummaryInfoGetPropertyCount(IntPtr summaryInfo, out uint propertyCount);

		/// <summary>
		/// PInvoke of MsiViewGetColumnInfo.
		/// </summary>
		/// <param name="view">Handle to view.</param>
		/// <param name="columnInfo">Column info.</param>
		/// <param name="record">Handle for returned record.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiViewGetColumnInfo", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiViewGetColumnInfo(IntPtr view, int columnInfo, out IntPtr record);

		/// <summary>
		/// PInvoke of MsiViewExecute.
		/// </summary>
		/// <param name="view">Handle of view to execute.</param>
		/// <param name="record">Handle to a record that supplies the parameters for the view.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiViewExecute", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiViewExecute(IntPtr view, IntPtr record);

		/// <summary>
		/// PInvoke of MsiViewFetch.
		/// </summary>
		/// <param name="view">Handle of view to fetch a row from.</param>
		/// <param name="record">Handle to receive record info.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiViewFetch", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiViewFetch(IntPtr view, out IntPtr record);

		/// <summary>
		/// PInvoke of MsiViewModify.
		/// </summary>
		/// <param name="view">Handle of view to modify.</param>
		/// <param name="modifyMode">Modify mode.</param>
		/// <param name="record">Handle of record.</param>
		/// <returns>Error code.</returns>
		[DllImport("msi.dll", EntryPoint="MsiViewModify", CharSet=CharSet.Unicode, ExactSpelling=true)]
		internal static extern uint MsiViewModify(IntPtr view, int modifyMode, IntPtr record);

		/// <summary>
		/// contains the file hash information returned by MsiGetFileHash and used in the MsiFileHash table.
		/// </summary>
		[StructLayout(LayoutKind.Explicit)]
			internal struct MSIFILEHASHINFO
		{
			[FieldOffset(0)] internal uint fileHashInfoSize;
			[FieldOffset(4)] internal int data0;
			[FieldOffset(8)] internal int data1;
			[FieldOffset(12)]internal int data2;
			[FieldOffset(16)]internal int data3;
		}
	}
}
