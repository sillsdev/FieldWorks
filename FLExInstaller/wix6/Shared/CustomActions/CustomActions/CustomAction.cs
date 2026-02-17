using System;
using System.Drawing;
using System.IO;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CheckAppPath(Session session)
        {
            //Referenced by CustomAction CheckApplicationPath
            session.Log("Checking Application Path.");
            string apppath = session["APPFOLDER"];
            string datapath = session["DATAFOLDER"];
            string dataFolderFound = session["DATAFOLDERFOUND"];
            char[] trimchars = new[] { Path.DirectorySeparatorChar };
            apppath = apppath.TrimEnd(trimchars);
            datapath = datapath.TrimEnd(trimchars);

            //set the message if WixUIValidatePath found an error.
            if (session["WIXUI_INSTALLDIR_VALID"] != "1")
                session["InvalidDirText"] = "Installation directory must be on a local hard drive.";

            //return failure if the app path is the program files directory
            string progFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFiles86Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            //On 32bit XP machines ProgramFilesX86 will return an empty string.
            if (String.IsNullOrEmpty(progFiles86Dir))
                progFiles86Dir = progFilesDir;

            //If both values point to the x86 dir, truncate one.
            if (progFiles86Dir.EndsWith(" (x86)") && progFilesDir.EndsWith(" (x86)"))
            {
                progFilesDir = progFilesDir.Substring(0, progFilesDir.IndexOf(" (x86)", StringComparison.OrdinalIgnoreCase));
            }
            //If neither value points to the x86 dir, concat x86 onto one.
            if (!progFiles86Dir.EndsWith(" (x86)") && !progFilesDir.EndsWith(" (x86)"))
            {
                progFiles86Dir = progFiles86Dir + " (x86)";
            }

            //Find the drive letter of the system volume
            string driveRoot = progFilesDir.Substring(0, progFilesDir.IndexOf(Path.DirectorySeparatorChar));

            CheckPath(session, apppath, driveRoot, progFilesDir, progFiles86Dir);

            //This change will only check the project folder path if it is a newly defined one.
            //If there already was a path defined in the registry then just accept it and move on.
            if (dataFolderFound == "NotFound")
                CheckPath(session, datapath, driveRoot, progFilesDir, progFiles86Dir);

            return ActionResult.Success;
        }

        private static void CheckPath(Session session, string path, string root, string pfDir, string pf86Dir)
        {
            //make sure the selected path is not one of these bad places to put stuff.
            if (path.EndsWith(":") ||
                path.Equals(pfDir, StringComparison.OrdinalIgnoreCase) ||
                path.Equals(pf86Dir, StringComparison.OrdinalIgnoreCase) ||
                path.Equals(root + Path.DirectorySeparatorChar + "Windows", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(root + Path.DirectorySeparatorChar + "Users", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(root + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar + "Public", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(root + Path.DirectorySeparatorChar + "i386", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.System), StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), StringComparison.OrdinalIgnoreCase))
            {
                session["InvalidDirText"] = String.Format("Cannot install to {0}.", path);
                session["WIXUI_INSTALLDIR_VALID"] = "0";
            }
        }

        [CustomAction]
        public static ActionResult VerifyDataDirPath(Session session)
        {
            //Referenced by CustomAction VerifyDataPath
            session.Log("Begin VerifyDataPath in custom action dll");
            string registryKey = session["REGISTRYDATAKEY"];
            string valueName = session["REGISTRYDATAVALUENAME"];
            string regDataPath = GetDataDirFromRegistry(registryKey, valueName, session);
            if (string.IsNullOrEmpty(regDataPath))
            {
                session["REGDATAFOLDER"] = null;
                session["DATAFOLDERFOUND"] = "NotFound";
                return ActionResult.Success;
            }

            session["REGDATAFOLDER"] = regDataPath;

            if (Directory.Exists(regDataPath) && Directory.GetFiles(regDataPath).Length > 0)
                session["DATAFOLDERFOUND"] = "AlreadyExisting";
            else
            {
                session["DATAFOLDERFOUND"] = "InvalidRegEntry";
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ClosePrompt(Session session)
        {
            //Referenced by CustomAction CloseApplications
            session.Log("Begin PromptToCloseApplications");

            try
            {
                var productName = session["ProductName"];
                var processes = session["PromptToCloseProcesses"].Split(',');
                var displayNames = session["PromptToCloseDisplayNames"].Split(',');

                if (processes.Length != displayNames.Length)
                {
                    session.Log(@"Please check that 'PromptToCloseProcesses' and 'PromptToCloseDisplayNames' exist and have same number of items.");
                    return ActionResult.Failure;
                }

                for (var i = 0; i < processes.Length; i++)
                {
                    session.Log("Prompting process {0} with name {1} to close.", processes[i], displayNames[i]);
                    using (var prompt = new PromptCloseApplication(session, productName, processes[i], displayNames[i]))
                        if (!prompt.Prompt())
                            return ActionResult.Failure;
                }
            }
            catch (Exception ex)
            {
                session.Log("Missing properties or wrong values. Please check that 'PromptToCloseProcesses' and 'PromptToCloseDisplayNames' exist and have same number of items. \nException:" + ex.Message);
                return ActionResult.Failure;
            }

            session.Log("End PromptToCloseApplications");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeleteVersionNumberFromRegistry(Session sessionb)
        {
            //Referenced by CustomAction DeleteRegistryVersionNumber
            string versionKeyPath = sessionb["REGISTRYVERSIONKEY"];
            string versionKeyWow = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" + versionKeyPath + "\\";
            string versionKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\" + versionKeyPath + "\\";

            try
            {
                if (RegistryU.KeyExists(versionKey))
                    RegistryU.DelKey(versionKey);
                if (RegistryU.KeyExists(versionKeyWow))
                    RegistryU.DelKey(versionKeyWow);
            }
            catch (Exception e)
            {
                sessionb.Log("Exception occured while deleting a registry key." + e.Message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult LookForInstalledFonts(Session session)
        {
            //Referenced by CustomAction SetFontValues
            //Check for each font that we want to install
            session["DOULOS_INSTALLED"] = DoesFontExist(session, "Doulos SIL", FontStyle.Regular).ToString();
            if (session["DOULOS_INSTALLED"].Equals("False"))
                File.Delete(@"C:\\Windows\\Fonts\\DoulosSIL-R.ttf");

            session["SBLHEB_INSTALLED"] = DoesFontExist(session, "SBL Hebrew", FontStyle.Regular).ToString();
            if (session["SBLHEB_INSTALLED"].Equals("False"))
                File.Delete(@"C:\\Windows\\Fonts\\SBL_Hbrw.ttf");

            session["CHARIS_INSTALLED"] = DoesFontExist(session, "Charis SIL", FontStyle.Regular).ToString();
            if (session["CHARIS_INSTALLED"].Equals("False"))
            {
                File.Delete(@"C:\\Windows\\Fonts\\CharisSIL-B.ttf");
                File.Delete(@"C:\\Windows\\Fonts\\CharisSIL-BI.ttf");
                File.Delete(@"C:\\Windows\\Fonts\\CharisSIL-I.ttf");
                File.Delete(@"C:\\Windows\\Fonts\\CharisSIL-R.ttf");
            }

            session["APPARATUS_INSTALLED"] = DoesFontExist(session, "Apparatus SIL", FontStyle.Regular).ToString();
            if (session["APPARATUS_INSTALLED"].Equals("False"))
            {
                File.Delete(@"C:\\Windows\\Fonts\\AppSILB.TTF");
                File.Delete(@"C:\\Windows\\Fonts\\AppSILBI.TTF");
                File.Delete(@"C:\\Windows\\Fonts\\AppSILI.TTF");
                File.Delete(@"C:\\Windows\\Fonts\\AppSILR.TTF");
            }

            session["GALATIA_INSTALLED"] = DoesFontExist(session, "Galatia SIL", FontStyle.Regular).ToString();
            if (session["GALATIA_INSTALLED"].Equals("False"))
            {
                File.Delete(@"C:\\Windows\\Fonts\\GalSILB.ttf");
                File.Delete(@"C:\\Windows\\Fonts\\GalSILR.ttf");
            }

            session["EZRA_INSTALLED"] = DoesFontExist(session, "Ezra SIL", FontStyle.Regular).ToString();
            if (session["EZRA_INSTALLED"].Equals("False"))
            {
                File.Delete(@"C:\\Windows\\Fonts\\SILEOT.ttf");
                File.Delete(@"C:\\Windows\\Fonts\\SILEOTSR.ttf");
            }

            return ActionResult.Success;
        }

        private static string GetDataDirFromRegistry(string path, string valueName, Session session)
        {
            string dataPathKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\" + path;
            string dataPathKeyWow = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" + path;

            string projPath = "";
            try
            {
                if (RegistryU.KeyExists(dataPathKey))
                    projPath = RegistryU.GetKey("HKLM", "SOFTWARE\\" + path).GetValue(valueName).ToString();
                if (RegistryU.KeyExists(dataPathKeyWow))
                    projPath = RegistryU.GetKey("HKLM", "SOFTWARE\\Wow6432Node\\" + path).GetValue(valueName).ToString();
            }
            catch (Exception ex)
            {
                session.Log(ex.Message);
            }
            return projPath;
        }

        public static bool DoesFontExist(Session session, string fontFamilyName, FontStyle fontStyle)
        {
            bool result;

            try
            {
                using (FontFamily family = new FontFamily(fontFamilyName))
                    result = family.IsStyleAvailable(fontStyle);
            }
            catch (ArgumentException)
            {
                result = false;
            }
            session.Log("Return Value for " + fontFamilyName + " : " + fontStyle.ToString() + " is " + result);

            return result;
        }
    }
}
