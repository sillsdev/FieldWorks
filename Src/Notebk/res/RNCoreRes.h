/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: RnCoreRes.h
Responsibility: Jeff Gayle
Last reviewed: never

Description:
	Main header file for all FieldWorks resource ID's.
----------------------------------------------------------------------------------------------*/
/*----------------------------------------------------------------------------------------------
	This file contains the "master" list of resource ID ranges for the Research Notebook. As a
	matter of proceedure, Research Notebook  IDs start at 2000 and work UP. There is a remote
	possibility that these number will collide with the IDs from the Application framework. Care
	should be taken to avoid this possibility by checking the src\AppCore\Res\AfCoreRes.h file
	before assigning a high range IDs. Do not pick a starting range much, if any, higher then
	the lim of the previous	range. Each range lim and min should be multiples of 100 or 50.

	This file should NEVER be included in any file. It only serves to document what IDs
	are being used.
----------------------------------------------------------------------------------------------*/

Do not remove this line. It intentionally causes a compiler error.


/*----------------------------------------------------------------------------------------------
	Status bar IDs.
----------------------------------------------------------------------------------------------*/
#define kstidStBar_Min				27900
#define kstidStBar_Lim				28000

// Add new expandable menu items in Resource.h
// Make sure they are between the range given by
// kcidMenuItemExpMin and kcidMenuItemExpLim in AfCoreRes.h (28900-29100).
//WARNING kcidMenuItemExpLim		29100



/*----------------------------------------------------------------------------------------------
	Resource Notebook Resource IDs
----------------------------------------------------------------------------------------------*/


// DO NOT ADD ANY RANGES AT THIS END.
// New ranges should be assigned at the bottom of the list since we are incrementing from 1000.


/*----------------------------------------------------------------------------------------------
	General RN app IDs
----------------------------------------------------------------------------------------------*/
#define kridBaseAppMin				1000
#define kridBaseAppLim				2200

/*----------------------------------------------------------------------------------------------
	RnImport IDs.
----------------------------------------------------------------------------------------------*/
#define kridRnImportMin				2600
#define kridRnImportLim				2900

/*----------------------------------------------------------------------------------------------
	RnEmptyNotebookDlg IDs
----------------------------------------------------------------------------------------------*/

#define kridEmptyNotebookMin        4660
#define kridEmptyNotebookLim        4700

/*----------------------------------------------------------------------------------------------
	Add next range here.
----------------------------------------------------------------------------------------------*/
