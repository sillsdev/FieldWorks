/*******************************************************************************
* Exceptions
*
* Description:
*   Generates exception messages for the system
*
* Dependencies:
*   spx_Create_Or_Alter_Exception
*
* Revision History:
*   19 April, 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header
*******************************************************************************/

/* TODO: Change names to match the triggers and tables */

EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_FW_Base',
  'FW Base Exception Message');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BI1_Class$_Ins_Table',
  'T_BI1_Class$_Ins Failed to Create Table');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BI1_Class$_Create_Trg',
  'T_BI1_Class$_Ins Failed to Create Trigger');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BD1_Class$_Del_Copy',
  'T_BD1_CmObject_Del copy to ObjListTbl failed');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BD1_Class$_Del_RI',
  'T_BD1_CmObject_Del Referential integrity violated');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BD2_CmObject_Upd_CmObject',
  'exc_T_BD2_CmObject Unable to update owning object');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_Id_Cannot_Be_Changed',
  'exc_Id_Cannot_Be_Changed An object Id cannot be changed');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BU0_CmObject_ValOwn_DOwn',
  'T_BU0_CmObject_ValOwn Duplicate owner');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BU0_CmObject_ValOwn_DSeq',
  'T_BU0_CmObject_ValOwn Duplicate sequence');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_T_BU0_CmObject_ValOwn_BOwn',
  'T_BU0_CmObject_ValOwn Bad owner');
EXECUTE PROCEDURE spx_Create_Or_Alter_Exception('exc_Update_Not_Allowed',
  'exc_Update_Not_Allowed Update not allowed');
COMMIT;
