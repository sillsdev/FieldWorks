/*******************************************************************************
* Build
*
* Description:
*   Builds a new FW database.
*
* Dependencies:
*   None
*
* Revision History:
*   8 May 2008, Steve Miller/Ann Bush: Created
*******************************************************************************/

/*==( Setup )==*/

/*SET ECHO ON;*/

/*==( Create the new db )==*/

SET SQL DIALECT 3;
CREATE DATABASE 'C:\Firebird\Data\FieldWorks\FW01.FDB'
	PAGE_SIZE 8192
	DEFAULT CHARACTER SET NONE;

/*==( Some DDL for DROP )==*/

INPUT 'spx_Drop_Dependencies.sql';
INPUT 'spx_Drop_External.sql';
INPUT 'spx_Drop_Table.sql';
INPUT 'spx_Drop_Generator.sql';
INPUT 'spx_Drop_All_Tables.sql';


/*==( Exceptions )==*/

INPUT 'spx_Create_Or_Alter_Exception.sql';
INPUT 'Exceptions.sql';
INPUT 'Exception_Messages.sql';

/*==( External UDFs )==*/

INPUT 'External_UDFs.sql';

/*==( Validation Procedures )==*/

INPUT 'spx_Valid_New_Constraint_Name.sql';
INPUT 'spx_Valid_New_Trigger_Name.sql';
INPUT 'spx_Valid_New_Proc_Name.sql';
INPUT 'spx_Valid_New_Table_Name.sql';
INPUT 'spx_Valid_New_Index_Name.sql';
INPUT 'spx_Valid_New_View_Name.sql';
INPUT 'spx_Valid_New_Param_Name.sql';

/*==( XML )==*/

/* TODO: This errors out when in script, but not stand-alone */
/* INPUT 'XML_To_Id.sql'; */

/*==( Metadata Tables and Views )==*/

INPUT 'Version$_table.sql';
INPUT 'AppCompat$_table.sql';
INPUT 'Module$_table.sql';
INPUT 'Class$_table.sql';
/*
INPUT 'Field$_table.sql';
INPUT 'ClassPar$_table.sql';
INPUT 'ObjRefs_table.sql';
INPUT 'ReplaceRef_table.sql';
INPUT 'PropInfo$_view.sql';
INPUT 'FlidCollation$_table.sql';
INPUT 'ObjListTbl$_table.sql';
INPUT 'ObjInfoTbl$_table.sql';
INPUT 'Sync$_table.sql';
INPUT 'CmObject_table.sql';
INPUT 'ObjInfoTbl$_Owned_view.sql';
INPUT 'ObjInfoTbl$_Ref_view.sql';
INPUT 'CmObject__view.sql';
*/

/*==( Metadada Procedures )==*/
/*
INPUT 'ClearSyncTable$_sp.sql';
INPUT 'StoreSyncRec$_sp.sql';
INPUT 'SetObjList$_sp.sql';
INPUT 'SetObjListClass_sp.sql';
INPUT 'CleanObjListTbl$_sp.sql';
INPUT 'CleanObjInfoTbl$_sp.sql';
INPUT 'DefineCreateProc.sql'; -- This is now GenMakeObjProc_sp.sql, I think
INPUT 'CreateDeleteObj_sp.sql';
INPUT 'UpdateClassView_sp.sql';
INPUT 'CreateGetRefsToObj_sp.sql';
INPUT 'DefineReplaceRefCollProc_sp.sql'; -- This is now GenReplRCProc_sp.sql, I think
INPUT 'DefineReplaceRefSeqProc_sp.sql'; -- This is now GenReplRSProc_sp.sql, I think
*/

/*==( Triggers )==*/
/*
INPUT 'TR_Class$_Ins_trig.sql;  -- Yves' is BI1_Class$_Ins.sp
-- Yves has here T_BI1_Class$_Ins.trg, which is currently in src\LangProj\TR_Class$_InsLast_trig.sql. I'm not sure we want it here.
-- Yves has here T_AI0_Class$_Ins.trg, which is currently in src\LangProj\TR_Class$_InsLast_trip.sql, I believe.
-- Yves has here T_AI1_Class$_Ins.trg, which I think is in src\LangProj\TR_Class$_InsLast_trip.sql.
*/

/* The rest of Yves' script:

#include <T_BD1_CmObject_Del.trg>
#include <T_BD2_CmObject_Del.trg>
#include <T_BU0_CmObject_ValOwn.trg>
#include <MultiStr$.tab>
#include <MultiBigStr$.tab>
#include <MultiBigTxt$.tab>
#include <BI1_Field$_Ins.sp>
#include <T_BI1_Field$_Ins.trg>
#include <T_AI0_Field$_Ins.trg>
#include <T_AI1_Field$_Ins.trg>
#include <CreateOwnedObject$.sp>
#include <CreateObject$.sp>
#include <AddCustomField$.sp>
#include <SetMultiBigStr$.sp>
#include <SetMultiBigTxt$.sp>
#include <SetMultiStr$.sp>
#include <SetMultiTxt$.sp>
*/

/*==( Cleanup )==*/

/*SET ECHO OFF;*/
EXIT;
