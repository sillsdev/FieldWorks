#include "Cellar.sqi"

#define CMCG_SQL_DEFNS
#include "Cellar.sqh"
#include "FeatSys.sqh"
#include "Notebk.sqh"
#include "Ling.sqh"
#include "Scripture.sqh"
#include "LangProj.sqh"

#include "LangProj.sqo"

// include what used to be in LangProjSP.sql
#include "FinalDatabaseCreation.sql"
#include "TR_Field$_UpdateModel_InsLast_trig.sql"
#include "TR_Class$_InsLast_trig.sql"
#include "ObjHierarchy$_table.sql"
#include "UpdateHierarchy_sp.sql"
#include "PageSetup$_table.sql"
#include "AlignBinary_fn.sql"
#include "GetOrderedMultiTxt_sp.sql"

// Triggers
#include "TR_CmSortSpec_Ins_trig.sql"
#include "TR_CmSortSpec_Del_trig.sql"
#include "TR_CmSortSpec_Upd_trig.sql"
#include "TR_StTxtPara_Owner_Ins_trig.sql"
#include "TR_StTxtPara_Owner_UpdDel_trig.sql"
#include "TR_StPara_Owner_Ins_trig.sql"
#include "TR_StPara_Owner_UpdDel_trig.sql"
#include "TR_StStyle_Ins_trig.sql"
#include "TR_StText_Owner_Ins_trig.sql"
#include "TR_StText_Owner_UpdDel_trig.sql"
#include "NoteInterlinProcessTime_sp.sql"

#include "../Notebk/NotebkSP.sql" // This uses definitions from both Notebk.sqh and LangProj.sqh
