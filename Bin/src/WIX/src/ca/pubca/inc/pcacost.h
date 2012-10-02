#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="cpiutilsched.h" company="Microsoft">
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
//    Costs for Public Custom Actions.
// </summary>
//-------------------------------------------------------------------------------------------------


#define COST_PARTITION_CREATE 10000
#define COST_PARTITION_DELETE 10000

#define COST_PARTITION_USER_CREATE 10000
#define COST_PARTITION_USER_DELETE 10000

#define COST_USER_IN_PARTITION_ROLE_CREATE 10000
#define COST_USER_IN_PARTITION_ROLE_DELETE 10000

#define COST_APPLICATION_CREATE 10000
#define COST_APPLICATION_DELETE 10000

#define COST_APPLICATION_ROLE_CREATE 10000
#define COST_APPLICATION_ROLE_DELETE 10000

#define COST_USER_IN_APPLICATION_ROLE_CREATE 10000
#define COST_USER_IN_APPLICATION_ROLE_DELETE 10000

#define COST_ASSEMBLY_REGISTER   50000
#define COST_ASSEMBLY_UNREGISTER 10000

#define COST_ROLLASSIGNMENT_CREATE 10000
#define COST_ROLLASSIGNMENT_DELETE 10000

#define COST_SUBSCRIPTION_CREATE 10000
#define COST_SUBSCRIPTION_DELETE 10000

#define COST_MESSAGE_QUEUE_CREATE 10000
#define COST_MESSAGE_QUEUE_DELETE 10000

#define COST_MESSAGE_QUEUE_PERMISSION_ADD    10000
#define COST_MESSAGE_QUEUE_PERMISSION_REMOVE 10000
