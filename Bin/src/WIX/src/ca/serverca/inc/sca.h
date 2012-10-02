#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="sca.h" company="Microsoft">
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
//    Shared header between scheduling and execution CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#define MAGIC_MULTISZ_CHAR 127

// Generic action enum.
enum SCA_ACTION
{
	SCA_ACTION_NONE,
	SCA_ACTION_INSTALL,
	SCA_ACTION_UNINSTALL
};


// IIS Metabase actions
enum METABASE_ACTION
{
	MBA_CREATEKEY,
	MBA_DELETEKEY,
	MBA_WRITEKEY,
	MBA_CREATEAPP,
};

// user creation attributes definitions
enum SCAU_ATTRIBUTES
{
	SCAU_DONT_EXPIRE_PASSWRD = 0x00000001,
	SCAU_PASSWD_CANT_CHANGE = 0x00000002,
	SCAU_PASSWD_CHANGE_REQD_ON_LOGIN = 0x00000004,
	SCAU_DISABLE_ACCOUNT = 0x00000008,
	SCAU_FAIL_IF_EXISTS = 0x00000010,
	SCAU_UPDATE_IF_EXISTS = 0x00000020,

	SCAU_DONT_REMOVE_ON_UNINSTALL = 0x00000100,
	SCAU_DONT_CREATE_USER = 0x00000200,
};

// sql database attributes definitions
enum SCADB_ATTRIBUTES
{
	SCADB_CREATE_ON_INSTALL = 0x00000001,
	SCADB_DROP_ON_UNINSTALL = 0x00000002,
	SCADB_CONTINUE_ON_ERROR = 0x00000004,
	SCADB_DROP_ON_INSTALL = 0x00000008,
	SCADB_CREATE_ON_UNINSTALL = 0x00000010,
	SCADB_CONFIRM_OVERWRITE = 0x00000020,
	SCADB_CREATE_ON_REINSTALL = 0x00000040,
	SCADB_DROP_ON_REINSTALL = 0x00000080,
};

// sql string/script attributes definitions
enum SCASQL_ATTRIBUTES
{
	SCASQL_EXECUTE_ON_INSTALL = 0x00000001,
	SCASQL_EXECUTE_ON_UNINSTALL = 0x00000002,
	SCASQL_CONTINUE_ON_ERROR = 0x00000004,
	SCASQL_ROLLBACK = 0x00000008,
	SCASQL_EXECUTE_ON_REINSTALL = 0x00000010,
};

// certificate attributes defintions
enum SCA_CERT_INSTALLED
{
	SCA_CERT_INSTALLED_FILE_PATH = 0xF0000000,
};
