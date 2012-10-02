//This code grabbed from codeproject.com and modified slightly
using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Security;



namespace Grep
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Grepper
	{
		//Option Flags
		private bool m_bRecursive;
		private bool m_bIgnoreCase;
		private bool m_bJustFiles;
		private bool m_bLineNumbers;
		private bool m_bCountLines;
		private string m_strRegEx;
		private string m_strFiles;
		private string m_strDir = Environment.CurrentDirectory;
		//ArrayList keeping the Files
		private ArrayList m_arrFiles = new ArrayList();

		//Properties
		public bool Recursive
		{
			get { return m_bRecursive; }
			set { m_bRecursive = value; }
		}

		public bool IgnoreCase
		{
			get { return m_bIgnoreCase; }
			set { m_bIgnoreCase = value; }
		}

		public bool JustFiles
		{
			get { return m_bJustFiles; }
			set { m_bJustFiles = value; }
		}

		public bool LineNumbers
		{
			get { return m_bLineNumbers; }
			set { m_bLineNumbers = value; }
		}

		public bool CountLines
		{
			get { return m_bCountLines; }
			set { m_bCountLines = value; }
		}

		public string RegEx
		{
			get { return m_strRegEx; }
			set { m_strRegEx = value; }
		}

		public string Files
		{
			get { return m_strFiles; }
			set { m_strFiles = value; }
		}
		public string RootDirectory
		{
			get { return m_strDir; }
			set { m_strDir = value; }
		}
		//Build the list of Files
		private void GetFiles(String strDir, String strExt, bool bRecursive)
		{
			//search pattern can include the wild characters '*' and '?'
			string[] fileList = Directory.GetFiles(strDir, strExt);
			for(int i=0; i<fileList.Length; i++)
			{
				if(File.Exists(fileList[i]))
					m_arrFiles.Add(fileList[i]);
			}
			if(bRecursive==true)
			{
				//Get recursively from subdirectories
				string[] dirList = Directory.GetDirectories(strDir);
				for(int i=0; i<dirList.Length; i++)
				{
					GetFiles(dirList[i], strExt, true);
				}
			}
		}

		//Search Function
		public string Search()
		{
			String strDir = m_strDir;
			//First empty the list
			m_arrFiles.Clear();
			//Create recursively a list with all the files complying with the criteria
			String[] astrFiles = m_strFiles.Split(new Char[] {','});
			for(int i=0; i<astrFiles.Length; i++)
			{
				//Eliminate white spaces
				astrFiles[i] = astrFiles[i].Trim();
				GetFiles(strDir, astrFiles[i], m_bRecursive);
			}
			//Now all the Files are in the ArrayList, open each one
			//iteratively and look for the search string
			String strResults = "";
			bool bEmpty = true;
			IEnumerator enm = m_arrFiles.GetEnumerator();
			while(enm.MoveNext())
			{
				try
				{
					bEmpty = SearchFile((string)enm.Current, ref strResults);
				}
				catch(SecurityException)
				{
					strResults += "\r\n" + (string)enm.Current + ": Security Exception\r\n\r\n";
				}
				catch(FileNotFoundException)
				{
					strResults += "\r\n" + (string)enm.Current + ": File Not Found Exception\r\n";
				}
			}
			if(bEmpty == true)
				strResults =("No matches found!");

			return strResults;
		}

		public bool SearchFile(string path, ref String results)
		{
			bool isEmpty= false;
			int iLine;
			int iCount;
			String strLine;
			StreamReader sr = File.OpenText(path);
			iLine = 0;
			iCount = 0;
			bool bFirst = true;
			while((strLine = sr.ReadLine()) != null)
			{
				iLine++;
				//Using Regular Expressions as a real Grep
				Match mtch;
				if(m_bIgnoreCase == true)
					mtch = Regex.Match(strLine, m_strRegEx, RegexOptions.IgnoreCase);
				else
					mtch = Regex.Match(strLine, m_strRegEx);
				if(mtch.Success == true)
				{
					isEmpty = false;
					iCount++;
					if(bFirst == true)
					{
						if(m_bJustFiles == true)
						{
							results += (string)path + "\r\n";
							break;
						}
						else
							results += (string)path + ": ";
						bFirst = false;
					}
					//Add the Line to Results string
					if(m_bLineNumbers == true)
						results += "  " + iLine + ": " + strLine + "\r\n";
					else
						results += "  " + strLine + "\r\n";
				}
			}
			sr.Close();
			if(bFirst == false)
			{
				if(m_bCountLines == true)
					results += "  " + iCount + " Lines Matched\r\n";
				//results += "\r\n";
			}
			return isEmpty;
		}
	}
}
