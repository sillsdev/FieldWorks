using System;
using System.Data;
//using DBInterface;



namespace DBProgram
{
	/// <summary>
	/// Summary description for App.
	/// </summary>
	class App
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string HelpStr =
				"DB provides simple commands for FieldWorks databases.\n" +
				"For most database operations files must be in the data directory,\n" +
				"which is assumed to be " + Globals.DbFolder + ". \n" +
				"Usage:\n"+
				" db dbnames -- lists all database names\n" +
				" db fwnames -- lists all FieldWorks database names\n" +
				" db attach dbname -- attaches a database\n" +
				" db detach dbname -- detaches a database\n" +
				" db copy olddbname newdbname -- copies a database\n" +
				" db rename olddbname newdbname -- renames a database\n" +
				" db backup dbname [backupname] -- backs up a database\n" +
				" db restore dbname [backupname] -- restores a database from a backup\n" +
				" db delete dbname -- PERMANENTLY deletes a database without warning!!\n" +
				" db start -- starts the SQL database server\n" +
				" db stop -- stops the SQL database server\n" +
				" db procs -- lists master stored procedures used by FW \n" +
				" db logins -- lists logins available in master\n" +
				" db initialize -- initializes the SQL database server and attaches .mdf files\n" +
				"     in data directory.\n" +
				" db shrink dbname -- minimize size of database files\n" +
				" db exec sqlfilename dbname -- execute query in sqlfilename on database dbname\n" +
				" db version dbname -- show current database version\n" +
				" db debug on|off dbname -- starts|stops debug trace in c:\\FwDebug.trc\n" +
				"     BE SURE TO STOP WHEN DONE\n" +
				" db tune on|off dbname -- starts|stops performance trace in c:\\FwPerform.trc\n" +
				"     BE SURE TO STOP WHEN DONE\n" +
				" db load dbname -- loads/replaces database from an xml file in data directory\n" +
				" db dump dbname -- dumps database dbname to an xml file in data directory\n" +
				" db validate dbname -- validates dbname.xml to temp.err in data directory\n" +
				" db sources dbname [outfilename] -- dumps SQL sources\n" +
				" db structure dbname [outfilename] -- dumps SQL table structure\n";

			if (args.GetLength(0) < 1)
			{
				Console.WriteLine(HelpStr);
				return;
			}

			try
			{
				switch (args[0])
				{
					case "dbnames":
						DBInterface.ListAllDatabases();
						break;

					case "fwnames":
						DBInterface.ListFwDatabases();
						break;

					case "attach":
						if (args.GetLength(0) == 2)
							DBInterface.AttachDB(args[1]);
						else
							Console.WriteLine("Please enter a database name or type -? for help");
						break;

					case "detach":
						if (args.GetLength(0) == 2)
							DBInterface.DetachDB(args[1]);
						else
							Console.WriteLine("Please enter a database name or type -? for help");
						break;

					case "copy":
						if (args.GetLength(0) == 3)
							DBInterface.CopyDB(args[1], args[2]);
						else
							Console.WriteLine("Please enter a source and target database name or type -? for help");
						break;

					case "rename":
						if (args.GetLength(0) == 3)
							DBInterface.RenameDB(args[1],args[2]);
						else
							Console.WriteLine("Please enter a source and target database name or type -? for help");
						break;

					case "backup":
						if (args.GetLength(0) == 3)
							DBInterface.BackupDB(args[1], args[2]);
						else if (args.GetLength(0) == 2)
							DBInterface.BackupDB(args[1], null);
						else
							Console.WriteLine("Please enter a database and a filename to backup to or type -? for help");
						break;

					case "restore":
						if (args.GetLength(0) == 3)
							DBInterface.RestoreDB(args[1], args[2]);
						else if (args.GetLength(0) == 2)
							DBInterface.RestoreDB(args[1], null);
						else
							Console.WriteLine("Please enter a database and a filename to restore from or type -? for help");
						break;

					case "delete":
						if (args.GetLength(0) == 2)
							DBInterface.DeleteDB(args[1]);
						else
							Console.WriteLine("Please enter a database to delete or type -? for help");
						break;

					case "start":
						DBInterface.StartDB();
						break;

					case "stop":
						DBInterface.StopDB();
						break;

					case "procs":
						DBInterface.ListProcs();
						break;

					case "logins":
						DBInterface.ListLogins();
						break;

					case "initialize":
						DBInterface.InitializeFW();
						break;

					case "shrink":
						if (args.GetLength(0) == 2)
							DBInterface.ShrinkDB(args[1]);
						else
							Console.WriteLine("Please enter a database name or type -? for help");
						break;

					case "exec":
						if (args.GetLength(0) == 3)
							DBInterface.ExecDB(args[1], args[2]);
						else
							Console.WriteLine("Please enter a filename and a database name or type -? for help");
						break;

					case "version":
						if (args.GetLength(0) == 2)
							DBInterface.GetDBVersion(args[1]);
						else
							Console.WriteLine("Please enter a database name or type -? for help");
						break;

					case "debug":
						if (args.GetLength(0) == 3)
							DBInterface.TraceDB(args[1], args[2], 0);
						else
							Console.WriteLine("Please enter a database and whether to start or stop tracing, or type -? for help");
						break;

					case "tune":
						if (args.GetLength(0) == 3)
							DBInterface.TraceDB(args[1], args[2], 1);
						else
							Console.WriteLine("Please enter a database and whether to start or stop tracing, or type -? for help");
						break;

					case "load":
						if (args.GetLength(0) == 2)
							DBInterface.LoadDB(args[1]);
						else
							Console.WriteLine("Please enter a database to load from an XML file or type -? for help");
						break;

					case "dump":
						if (args.GetLength(0) == 2)
							DBInterface.DumpDB(args[1]);
						else
							Console.WriteLine("Please enter a database to dump to an XML file or type -? for help");
						break;

					case "validate":
						if (args.GetLength(0) == 2)
							DBInterface.ValidateXml(args[1]);
						else
							Console.WriteLine("Please enter an xml file to validate (without .xml) or type -? for help");
						break;

					case "sources":
						if (args.GetLength(0) == 3)
							DBInterface.DumpSources(args[1], args[2]);
						else if (args.GetLength(0) == 2)
							DBInterface.DumpSources(args[1], null);
						else
							Console.WriteLine("Please enter a database and a filename to dump sources to or type -? for help");
						break;

					case "structure":
						if (args.GetLength(0) == 3)
							DBInterface.DumpStructure(args[1], args[2]);
						else if (args.GetLength(0) == 2)
							DBInterface.DumpStructure(args[1], null);
						else
							Console.WriteLine("Please enter a database and a filename to dump structure to or type -? for help");
						break;

					default:
						Console.WriteLine(HelpStr);
						break;
				}
			}
			catch (Exception oEx)
			{
				Console.WriteLine("Error: " + oEx.Message);
			}
		}
	}
}
