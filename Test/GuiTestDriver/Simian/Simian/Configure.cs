using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Simian
{
	class Configure
	{
		private static string m_ConfigFile = "SimianConfig.xml";
		private static Configure m_config = null;
		private static GuiModel  m_guiModel = null;

		private string m_modelPath = null;
		private string m_modelRootFile = null;

		private string m_logFile = null;

		private string m_exePath = null;
		private string m_exeName = null;
		private string m_exeArgs = null;
		private string m_exeWork = null;
		private string m_exeData = null;

		/// <summary>
		/// Gets the only configuration if there is one (use null parameter value).
		/// If there isn't one, it sets one up.
		/// A default configuration is used when a null file name is given.
		/// </summary>
		/// <param name="ConfigFile">The name of the configuration XML file or null</param>
		public static Configure getOnly(string ConfigFile)
		{
			if (m_config == null) m_config = new Configure(ConfigFile);
			return m_config;
		}

		/// <summary>
		/// Sets up the Simian. A default configuration is used
		/// when a null file name is given.
		/// </summary>
		/// <param name="ConfigFile">The name of the configuration XML file or null</param>
		private Configure(string ConfigFile)
		{
			if (ConfigFile != null && ConfigFile.Length > 0 && ConfigFile != "")
				m_ConfigFile = ConfigFile;
			ReadConfig();
		}

		/// <summary>
		/// Gets the configured GuiModel.
		/// </summary>
		/// <returns>The guiModel referenced in the configuration.</returns>
		public GuiModel getGuiModel()
		{
			if (m_guiModel == null)
				m_guiModel = new GuiModel(m_modelPath, m_modelRootFile);
			return m_guiModel;
		}

		/// <summary>
		/// Gets the log file name from the configuration file.
		/// </summary>
		/// <returns>The name of the log file</returns>
		public string getLogFile() { return m_logFile; }

		/// <summary>
		/// Gets the application path from the configuration file.
		/// </summary>
		/// <returns>The path of the application</returns>
		public string getExePath() { return m_exePath; }

		/// <summary>
		/// Gets the application name from the configuration file.
		/// </summary>
		/// <returns>The name of the application</returns>
		public string getExeName() { return m_exeName; }

		/// <summary>
		/// Gets the application arguments from the configuration file.
		/// </summary>
		/// <returns>The arguments of the application</returns>
		public string getExeArgs() { return m_exeArgs; }

		/// <summary>
		/// Gets the application working directory from the configuration file.
		/// </summary>
		/// <returns>The working directory of the application</returns>
		public string getWorkDir() { return m_exeWork; }

		/// <summary>
		/// Gets the application database from the configuration file.
		/// </summary>
		/// <returns>The database to be used with the application</returns>
		public string getDataBase() { return m_exeData; }

		/// <summary>
		/// Reads the configuration xml file, stores the data and closes it.
		/// </summary>
		private void ReadConfig()
		{
			XmlElement config = XmlFiler.getDocumentElement(m_ConfigFile, "simian-config", true);
			// pull config data out, assign, close
			// the model path and root file is specified in the config file, read it.
			XmlElement log = config["log"];
			string logPath = log.GetAttribute("path");
			string logFile = log.GetAttribute("name");
			m_logFile = logPath + @"\" + logFile;
			XmlElement exe = config["application"];
			m_exePath = exe.GetAttribute("path");
			m_exeName = exe.GetAttribute("name");
			m_exeArgs = exe.GetAttribute("args");
			m_exeWork = exe.GetAttribute("work");
			m_exeData = exe.GetAttribute("db");
			XmlElement model = exe["model"];
			m_modelPath = model.GetAttribute("path");
			m_modelRootFile = model.GetAttribute("root");

		}

	}
}
