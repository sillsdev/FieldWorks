using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Resources;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace HelpTopicsChecker
{
	public partial class HelpTopicsCheckerSetupDlg : Form
	{
		string m_reportFile = "HelpTopicsCheckerResults.htm";
		public HelpTopicsCheckerSetupDlg()
		{
			InitializeComponent();
			Init();
		}
		private void Init()
		{
			// Load HelpTopicsDomains
			LoadHelpTopicsDomains();
			UpdateChmTextBox();
			LoadDefaultHelpsCheckerReportFolder();
		}

		private void LoadHelpTopicsDomains()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load("HelpTopicsDomains.xml");

			foreach (XmlNode helpTopicDomain in doc.SelectSingleNode("HelpTopicsDomains").ChildNodes)
			{
				HelpTopicsDomainResourceHelper htd = new HelpTopicsDomainResourceHelper(helpTopicDomain);
				helpTopicDomainCombo.Items.Add(htd);
			}
			helpTopicDomainCombo.SelectedIndexChanged += new EventHandler(helpTopicDomainCombo_SelectedIndexChanged);
			if (helpTopicDomainCombo.Items.Count > 0)
			{
				helpTopicDomainCombo.SelectedIndex = 0;
			}

		}

		void helpTopicDomainCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (helpTopicDomainCombo.SelectedIndex >= 0)
			{
				HelpTopicsDomainResourceHelper htdrh = helpTopicDomainCombo.SelectedItem as HelpTopicsDomainResourceHelper;
				chmFileTextBox.Text = htdrh.GetDefaultChmFile();
				LoadDefaultReportFilename();
				LoadDefaultHelpFilesFolder();
			}
		}

		private void LoadDefaultHelpFilesFolder()
		{
			SetHelpFolderTextBoxLabel();
			if (radioButton_Yes.Checked)
			{
				HelpTopicsDomainResourceHelper htdrh = helpTopicDomainCombo.SelectedItem as HelpTopicsDomainResourceHelper;
				helpFolderTextBox.Text = System.IO.Directory.GetParent(chmFileTextBox.Text).ToString() + @"\Src-" + htdrh.DomainName;
			}
			else
			{
				helpFolderTextBox.Text = FieldWorksDirectoryFinder.RootDir;
			}
		}

		private void LoadDefaultHelpsCheckerReportFolder()
		{
			resultsFolderTextBox.Text = FieldWorksDirectoryFinder.FWProgramDirectory;
		}

		private void LoadDefaultReportFilename()
		{
			HelpTopicsDomainResourceHelper htdrh = helpTopicDomainCombo.SelectedItem as HelpTopicsDomainResourceHelper;
			m_reportFile = String.Format(saveFileDialog.Tag.ToString(), htdrh.DomainName);
			label_resultsFolder.Text = String.Format(label_resultsFolder.Tag.ToString(), m_reportFile);
		}

		private void button_Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void radioButton_Yes_CheckedChanged(object sender, EventArgs e)
		{
			UpdateChmTextBox();
			LoadDefaultHelpFilesFolder();
		}

		private void UpdateChmTextBox()
		{
			if (radioButton_Yes.Checked == true)
			{
				chmFileTextBox.Enabled = true;
				button_chmFileFinder.Enabled = true;
			}
			else
			{
				chmFileTextBox.Enabled = false;
				button_chmFileFinder.Enabled = false;
			}
		}

		private void SetHelpFolderTextBoxLabel()
		{
			if (radioButton_Yes.Checked == true)
				label_helpFolder.Text = String.Format(label_helpFolder.Tag.ToString(), new string[] {"Target", "decompiled"});
			else
				label_helpFolder.Text = String.Format(label_helpFolder.Tag.ToString(), new string[] {"Source", "uncompiled"});
		}

		private void button_chmFileFinder_Click(object sender, EventArgs e)
		{
			if (System.IO.File.Exists(chmFileTextBox.Text))
			{
				chmFileFinderDlg.InitialDirectory = System.IO.Directory.GetParent(chmFileTextBox.Text).ToString();
			}
			if (chmFileFinderDlg.ShowDialog() == DialogResult.OK)
			{
				chmFileTextBox.Text = chmFileFinderDlg.FileName;
			}
		}

		private void DecompileChm(string folder, string chm)
		{
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.EnableRaisingEvents = false;
			proc.StartInfo.FileName = "Hh.exe";
			proc.StartInfo.Arguments = String.Format("-decompile {0} {1}",
				new object[] { folder, chm });
			MessageBox.Show("about to run: " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
			proc.Start();
			proc.WaitForExit();
		}

		private DialogResult SetupForSavingReport(string foldername, string resultsFile)
		{
			string caption;
			string message;
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result = DialogResult.None;
			if (!System.IO.Directory.Exists(foldername))
			{
				caption = "Create directory?";
				message = foldername + " does not exist. \n\nCreate?";
				result = MessageBox.Show(message, caption, buttons);
				if (result != DialogResult.Yes)
				{
					return result;
				}
			}
			if (System.IO.File.Exists(foldername + @"\" + resultsFile))
			{
				caption = "Overwrite file?";
				message = resultsFile + " already exists. \n\nOverwrite?";
				result = MessageBox.Show(message, caption, buttons);
				if (result != DialogResult.Yes)
				{
					return result;
				}
			}
			return result;
		}

		private DialogResult SetupDirectoryForChm()
		{
			string caption;
			string message;
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result = DialogResult.None;
			if (!System.IO.Directory.Exists(helpFolderTextBox.Text))
			{
				caption = "Create Directory?";
				message = helpFolderTextBox.Text + " directory does not exist. \n\nCreate?";
				result = MessageBox.Show(message, caption, buttons);
				if (result == DialogResult.Yes)
				{
					System.IO.Directory.CreateDirectory(helpFolderTextBox.Text);
				}
			}
			else
			{
				caption = "Clear Directory?";
				message = helpFolderTextBox.Text + " directory exists. \n\nClear all its files and subdirectories?";
				result = MessageBox.Show(message, caption, buttons);
				if (result == DialogResult.Yes)
				{
					foreach (string strDir in Directory.GetDirectories(helpFolderTextBox.Text, "*", SearchOption.TopDirectoryOnly)) //deleting all files
						Directory.Delete(strDir, true);
					foreach (string strFile in Directory.GetFiles(helpFolderTextBox.Text, "*", SearchOption.TopDirectoryOnly)) //deleting all files
						File.Delete(strFile);
				}
			}
			return result;
		}

		private void button_GenerateReport_Click(object sender, EventArgs e)
		{
			// DialogResult.None
			DialogResult result = DialogResult.None;
			result = SetupForSavingReport(resultsFolderTextBox.Text, m_reportFile);
			if (result != DialogResult.Yes && result != DialogResult.None)
			{
				// let the user continue to configure things;
				return;
			}
			if (chmFileTextBox.Enabled)
			{
				// First decompile the specified chm file.
				result = SetupDirectoryForChm();
				if (result != DialogResult.Yes && result != DialogResult.None)
				{
					// let the user continue to configure things.
					return;
				}
				DecompileChm(helpFolderTextBox.Text, chmFileTextBox.Text);
			}

			HelpTopicsDomainResourceHelper htdrh = helpTopicDomainCombo.SelectedItem as HelpTopicsDomainResourceHelper;
			htdrh.ChmFile = chmFileTextBox.Enabled ? chmFileTextBox.Text : null;
			htdrh.CheckHelpFilePathsAgainstHelpFiles(helpFolderTextBox.Text);
			htdrh.GetHelpFilesNotFoundInHelpTopicPaths(helpFolderTextBox.Text);
			htdrh.SaveReport(resultsFolderTextBox.Text, m_reportFile);
			ShowReport(resultsFolderTextBox.Text, m_reportFile);
			this.Close();
		}

		private void ShowReport(string folder, string reportFile)
		{
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.StartInfo.FileName = folder + @"\" + reportFile;
			proc.Start();
		}

		private void button_baseHelpFolderFinder_Click(object sender, EventArgs e)
		{
			baseHelpFolderFinderDlg.Description = String.Format(baseHelpFolderFinderDlg.Tag.ToString(), radioButton_Yes.Checked ? "target" : "source");
			if (System.IO.Directory.Exists(helpFolderTextBox.Text))
			{
				baseHelpFolderFinderDlg.SelectedPath = helpFolderTextBox.Text;
			}
			if (baseHelpFolderFinderDlg.ShowDialog() == DialogResult.OK)
			{
				helpFolderTextBox.Text = baseHelpFolderFinderDlg.SelectedPath;
			}
		}

		private void button_reportFolderFinder_Click(object sender, EventArgs e)
		{
			reportFolderFinderDlg.Description = String.Format(reportFolderFinderDlg.Tag.ToString(), m_reportFile);
			if (System.IO.Directory.Exists(resultsFolderTextBox.Text))
			{
				reportFolderFinderDlg.SelectedPath = resultsFolderTextBox.Text;
			}
			if (reportFolderFinderDlg.ShowDialog() == DialogResult.OK)
			{
				resultsFolderTextBox.Text = reportFolderFinderDlg.SelectedPath;
			}
		}
	}

	internal class HelpTopicPathsResourceHelper
	{
		XmlNode node;
		ResourceManager helpResources;
		internal HelpTopicPathsResourceHelper(XmlNode helpTopicPathsNode)
		{
			node = helpTopicPathsNode;
			helpResources = new System.Resources.ResourceManager(this.ResourceKey,
				Assembly.LoadFile(FieldWorksDirectoryFinder.FWProgramDirectory + @"\" + this.AssemblyName));
			helpResources.IgnoreCase = true;
		}
		private string AssemblyName
		{
			get { return node.Attributes["assemblyName"].Value; }
		}
		private string ResourceKey
		{
			get { return node.Attributes["resourceKey"].Value; }
		}

		private string DefaultChmFileKey
		{
			get { return "UserHelpFile"; }
		}
		private string ChmFileKey
		{
			get
			{
				XmlAttribute xa = node.Attributes["chmFileKey"];
				if (xa == null)
					return DefaultChmFileKey;
				else
					return xa.Value;
			}
		}

		internal string GetDefaultChmFile()
		{
			string relPath = null;
			try
			{
				relPath = helpResources.GetString(this.ChmFileKey, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch
			{
			}
			if (relPath == null)
			{
				// try current culture
				relPath = helpResources.GetString(this.ChmFileKey, System.Globalization.CultureInfo.CurrentCulture);
			}
			return System.IO.Path.GetFullPath(FieldWorksDirectoryFinder.FWInstallDirectory + relPath);
		}

		internal void CheckHelpFilePathsAgainstHelpFiles(string helpFileDir, ref Hashtable helpFilesInResourceTables, ref ArrayList brokenLinks)
		{
			// Create an IDictionaryEnumerator to iterate through the resources.
			ResourceSet rs = helpResources.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, false);

			// identify any files referenced in our Resource file that we can't find in the
			// help files.
			foreach (DictionaryEntry d in rs)
			{
				if (d.Value == null ||
					d.Key.ToString() == this.ChmFileKey ||
					d.Key.ToString() == this.DefaultChmFileKey)
				{
					continue;
				}
				string html = helpFileDir + @"\" + d.Value.ToString();
				helpFilesInResourceTables[d.Value.ToString().ToLower()] = GetDefaultChmFile();
				if (!System.IO.File.Exists(html))
				{
					brokenLinks.Add(d.Value.ToString());
				}
			}
		}

	}

	internal class HelpTopicsDomainResourceHelper
	{
		XmlNode node;
		ArrayList helpTopicPathsNodeList;
		Hashtable helpFilesInResourceTables = new Hashtable();
		// Report
		string urlPrepend = "mk:@MSITStore:";
		string m_chmFile = null;
		XmlDocument report = new XmlDocument();

		internal HelpTopicsDomainResourceHelper(XmlNode helpTopicDomain)
		{
			node = helpTopicDomain;
			LoadHelpTopicPaths(node.SelectNodes("HelpTopicPaths"));
			report.LoadXml("<html><body></body></html>");
		}
		private void LoadHelpTopicPaths(XmlNodeList nodeList)
		{
			helpTopicPathsNodeList = new ArrayList(nodeList.Count);
			foreach (XmlNode node in nodeList)
			{
				helpTopicPathsNodeList.Add(new HelpTopicPathsResourceHelper(node));
			}
		}
		public override string ToString()
		{
			return node.Attributes["label"].Value;
		}

		internal string GetDefaultChmFile()
		{
			if (helpTopicPathsNodeList.Count > 0)
				return HelpTopicPathsResourceHelperList[0].GetDefaultChmFile();
			else
				return FieldWorksDirectoryFinder.FWInstallDirectory;
		}

		internal string DomainName
		{
			get { return node.Name; }
		}
		internal string ChmFile
		{
			get { return m_chmFile; }
			set { m_chmFile = value; }
		}

		private void AddLinksToReport(string sectionDescription, ArrayList links, string baseDir, string chmFile)
		{
			//Console.WriteLine("");
			XmlNode body = report.SelectSingleNode("/html/body");
			XmlElement b = report.CreateElement("b");
			body.AppendChild(b);
			XmlElement p1 = report.CreateElement("p");
			p1.InnerText = sectionDescription;
			b.AppendChild(p1);
			XmlElement p2 = report.CreateElement("p");
			b.AppendChild(p2);
			//Console.WriteLine(b.ToString());
			foreach (string link in links)
			{
				XmlElement a = report.CreateElement("a");
				string hrefVal;
				if (!String.IsNullOrEmpty(chmFile))
				{
					hrefVal = urlPrepend + chmFile + "::" + link;
				}
				else
				{
					hrefVal = System.IO.Path.GetFullPath(baseDir + @"\" + link);
				}
				a.SetAttribute("href", hrefVal);
				a.InnerText = hrefVal;
				body.AppendChild(a);
			//	Console.WriteLine(a.ToString());
				XmlElement lineBreak = report.CreateElement("br");
				body.AppendChild(lineBreak);
			}

			//Console.WriteLine("");
		}

		internal void CheckHelpFilePathsAgainstHelpFiles(string helpFileDir)
		{
			helpFilesInResourceTables.Clear();
			ArrayList brokenLinks = new ArrayList();
			foreach (HelpTopicPathsResourceHelper htprh in HelpTopicPathsResourceHelperList)
			{
				htprh.CheckHelpFilePathsAgainstHelpFiles(helpFileDir, ref helpFilesInResourceTables, ref brokenLinks);
			}

			string sectionDescription = this.ToString() + " (possibly) has " + brokenLinks.Count.ToString() + " broken chm links.";
			brokenLinks.Sort();
			AddLinksToReport(sectionDescription, brokenLinks, helpFileDir, m_chmFile);
		}

		internal void GetHelpFilesNotFoundInHelpTopicPaths(string helpFileDir)
		{
			// Now identify all the files that are in the chm, but not in the resource files.
			ArrayList filesMissingFromResource = new ArrayList();
			ArrayList filesMatchingResource = new ArrayList();
			ArrayList filesFilteredOut = new ArrayList();
			Regex exclusionPattern = new Regex("overview\\.htm$");
			foreach (string longfile in Directory.GetFiles(helpFileDir, "*.htm", SearchOption.AllDirectories))
			{
				// first convert file format to link format
				string relativePath = longfile.Substring(helpFileDir.Length + 1);
				string file = relativePath.Replace('\\', '/');

				if (exclusionPattern.Match(file.ToLower()).Success)
				{
					filesFilteredOut.Add(file);
					continue;
				}
				object fileFound = helpFilesInResourceTables[file.ToLower()];
				if (fileFound == null)
				{
					filesMissingFromResource.Add(file);
				}
				else
				{
					filesMatchingResource.Add(file);
				}
			}

			string sectionDescription = this.ToString() + " has no direct help paths to " +
				filesMissingFromResource.Count.ToString() + " help files.";
			filesMissingFromResource.Sort();
			//foreach (string file in filesMissingFromResource)
			//{
			//    string relativePath = file.Substring(helpFileDir.Length);
			//    file.Replace(file, relativePath.Replace('\\', '/'));
			//}
			AddLinksToReport(sectionDescription, filesMissingFromResource, helpFileDir, null);

			sectionDescription = this.ToString() + ": The following " + filesFilteredOut.Count +
				" help files were excluded from this report\n" +
				" because they matched the Regular expression pattern: /" + exclusionPattern.ToString() + "/";
			filesFilteredOut.Sort();
			//foreach (string file in filesFilteredOut)
			//{
			//    string relativePath = file.Substring(helpFileDir.Length);
			//    file.Replace(file, relativePath.Replace('\\', '/'));
			//}
			AddLinksToReport(sectionDescription, filesFilteredOut, helpFileDir, null);
		}

		internal void SaveReport(string foldername, string resultsFile)
		{
			//Console.WriteLine(report.OuterXml);
			XmlWriter reportWriter = XmlWriter.Create(foldername + @"\" + resultsFile);
			report.Save(reportWriter);
		}

		private HelpTopicPathsResourceHelper[] HelpTopicPathsResourceHelperList
		{
			get { return (HelpTopicPathsResourceHelper[])helpTopicPathsNodeList.ToArray(typeof(HelpTopicPathsResourceHelper)); }
		}
	}

}