/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DbVersion.h
Responsibility: Alistair Imrie, Tom Bogle, Steve Miller, Tim Steenwyk
Last reviewed: Today

	This is the db version that is compatible with this app version.  This number should match
	DbVer in Version$ table. It consists of
	VVVMMDDD where
	VVV is the main version number just released.
	MM is the milestone we are working on.
	DDD is a number that is incremented whenever changes are made to the database that affect
			C++/C# code.
	(Version numbers for each product are generated in the mk*.bat files.)

-------------------------------------------------------------------------------*//*:End Ignore*/

// This was moved from AfCore.h so that C# could use it.
// It was then changed from a #define so that C# could read it.
// This should always be kept in sync with Version$ in Src\Cellar\Version$_table.sql
enum DbVersion
{
	/***************************** NOTICE **************************************
	// Every migration change must now include the following:
	//
	// 1. Change the version number here.
	// 2. Change the version number in Src\Cellar\Version$_table.sql.
	// 3. Change the Wix Installer (See Installer\Use of the Wix Installer
	// System.doc for more info.)
	// 	3.1. Check out:
	// 			Installer\Features.wxs
	// 			Installer\FileLibrary.xml
	// 			Installer\Files.wxs
	// 	3.2. Run TestWixInstallerIntegrity.bat (I like to run in a command
	// 	     window so I can see what's happening.) Since you have a data
	// 		 migration script, there is no need to check
	// 	     Installer\ChangedFiles.xml.
	// 	3.3. In Internet Explorer, run Installer\Update Files.htm. Because
	// 		 it uses an Active Script, it doesn't work in FireFox. For your
	// 		 data migration script, check: DN, FLEX, TE, and TLE. Press
	// 		 the Save Changes button at the bottom.
	//	4. (In the final testing stages, anyway...) After the tests are run and
	//		everything checks out: check out Sena 2 and Sena 3,	copy them
	//		wherever they need to go, attach them, and open them with either
	//		FLEx or TE to migrate them. The following instructions are from Ken:
	//
	//		We need to be very careful any time we update the Sena databases in
	//		Perforce because of the potential for messing up the default writing
	//		systems. If you have any language xml files on your machine when the
	//		Sena files are opened, it will probably copy the contents of those
	//		xml files into the master Sena databases. This can result in the
	//		wrong fonts being set or the wrong valid characters being stored. To
	//		avoid this problem,use a process similar to the following:
	//
	//		<Check out Sena files in P4>
	//		db delete “Sena 2”
	//		db delete “Sena 3”
	//		copy c:\fw?\DistFiles\ReleaseData\*.* C:\Program Files\Microsoft SQL Server\MSSQL.?\MSSQL\Data
	//		db attach “Sena 2”
	//		db attach “Sena 3”
	//		nant IcuData
	//		<Make whatever minimal changes are needed in Sena databases.  Note that it's usually safer to apply
	//               a migration script through the SQL Server management studio than with Flex.>
	//		db shrink “Sena 2”
	//		db shrink “Sena 3”
	//		db detach “Sena 2”
	//		db detach “Sena 3”
	//		copy C:\Program Files\Microsoft SQL Server\MSSQL.?\MSSQL\Data\Sena 2.* c:\fw?\DistFiles\ReleaseData
	//		copy C:\Program Files\Microsoft SQL Server\MSSQL.?\MSSQL\Data\Sena 3.* c:\fw?\DistFiles\ReleaseData
	//		db attach “Sena 2”
	//		db attach “Sena 3”
	//		<I usually open MSSMS at this point and run “select * from LgWritingSystem”
	//		on the Sena dbs at this point to verify that everything looks OK.>
	//		<Check files in to P4>
	//
	//	5. If necessary, update the data to create the TestLangProj and
	//		Lela-Teli projects. Cherk out and modify:
	//
	//			Test\Lela-Teli2.xml
	//			Test\Lela-Teli3.xml
	//			Test\TestLangProj.zip
	//			DistFiles\Templates\NewLangProj.xml
	/**************************************************************************/

	// The following summary line is needed for C#
	/// <summary>The database version that is compatible with this app version.</summary>


	kdbAppVersion = 200261

};
