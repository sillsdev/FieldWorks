// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.Xml;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// Main window for the UnicodeCharEditor program.
	/// </summary>
	public partial class CharEditorWindow : Form, IHelpTopicProvider
	{
		private static ResourceManager s_helpResources;

		private string m_sHelpTopic = "khtpUnicodeEditorCharTab";
		bool m_fDirty;
		/// <summary>Characters that have overrides by this user (from CustomChars.xml)</summary>
		readonly Dictionary<int, PUACharacter> m_dictCustomChars = new Dictionary<int, PUACharacter>();
		/// <summary>Characters that have overrides from standard Unicode (from UnicodeDataOverrides.txt)</summary>
		readonly Dictionary<int, PUACharacter> m_dictModifiedChars = new Dictionary<int, PUACharacter>();

		private sealed class PuaListItem : ListViewItem
		{
			readonly int m_code;
			internal PuaListItem(PUACharacter spec)
				: base(GetListViewSubItemsArray(spec))
			{
				Tag = spec;
				int.TryParse(spec.CodePoint, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out m_code);
			}

			internal int Code => m_code;
		}

		private sealed class PuaListItemComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				var pli1 = x as PuaListItem;
				var pli2 = y as PuaListItem;
				if (pli1 != null && pli2 != null)
				{
					return pli1.Code.CompareTo(pli2.Code);
				}
				int code1;
				int code2;
				if (int.TryParse(x.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code1) &&
					int.TryParse(y.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code2))
				{
					return code1.CompareTo(code2);
				}
				return x.ToString().CompareTo(y.ToString());
			}
		}

		/// <summary />
		public CharEditorWindow()
		{
			InitializeComponent();

			m_lvCharSpecs.Sorting = SortOrder.Ascending;
		}


		/// <summary>
		/// Add existing PUA characters to the table at load time.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			ReadDataFromUnicodeFiles();
			ReadCustomCharData();
			foreach (var spec in m_dictCustomChars.Values)
			{
				var lvi = new PuaListItem(spec);
				m_lvCharSpecs.Items.Add(lvi);
			}
			m_lvCharSpecs.ListViewItemSorter = new PuaListItemComparer();
			m_fDirty = false;
		}

		private static string[] GetListViewSubItemsArray(PUACharacter cs)
		{
			var rgs = new string[10];
			rgs[0] = cs.CodePoint;
			rgs[1] = cs.Name;
			rgs[2] = cs.GeneralCategory.ToString();
			rgs[3] = cs.CanonicalCombiningClass.ToString();
			rgs[4] = cs.BidiClass.ToString();
			rgs[5] = cs.Decomposition;
			rgs[6] = cs.BidiMirrored.ToString();
			rgs[7] = cs.Upper;
			rgs[8] = cs.Lower;
			rgs[9] = cs.Title;
			return rgs;
		}

		private void ReadDataFromUnicodeFiles()
		{
			var icuDir = CustomIcu.DefaultDataDirectory;
			if (string.IsNullOrEmpty(icuDir))
			{
				throw new Exception("An error occurred: ICU directory not found. Registry value for ICU not set?");
			}
			var unicodeDataFilename = Path.Combine(icuDir, "UnicodeDataOverrides.txt");
			if (!File.Exists(unicodeDataFilename))
			{
				return;
			}
			using (var reader = File.OpenText(unicodeDataFilename))
			{
				while (reader.Peek() >= 0)
				{
					var sLine = ReadLineAndRemoveComment(reader);
					if (string.IsNullOrEmpty(sLine))
					{
						continue;
					}
					var idx = sLine.IndexOf(';');
					if (idx <= 0)
					{
						continue;
					}
					var sCode = sLine.Substring(0, idx).Trim();
					if (string.IsNullOrEmpty(sCode))
					{
						continue;
					}
					var sProps = sLine.Substring(idx + 1).Trim();
					if (sProps.StartsWith("<") && sProps.Contains("Private Use"))
					{
						continue;
					}
					int code;
					int codeMax;
					if (!ParseCodeField(sCode, out code, out codeMax))
					{
						continue;
					}

					if (code != codeMax)
					{
						continue;
					}
					var dataProperties = sProps.Split(';');
					if (dataProperties.Length == PUACharacter.ExectedPropCount + 1)
					{
						// One extra is OK...I think it comes from a comment in the SIL PUA properties.
						// But the PUACharacter constructor doesn't like it, so strip it off.
						var ich = sProps.LastIndexOf(';');
						sProps = sProps.Substring(0, ich);
					}
					var charSpec = new PUACharacter(sCode, sProps);
					m_dictModifiedChars.Add(code, charSpec);
				}
			}
		}

		private static bool ParseCodeField(string sCode, out int code, out int codeMax)
		{
			code = 0;
			codeMax = 0;
			if (string.IsNullOrEmpty(sCode))
			{
				return false;
			}
			sCode = sCode.Trim();
			if (string.IsNullOrEmpty(sCode))
			{
				return false;
			}
			if (int.TryParse(sCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
			{
				codeMax = code;
				return true;
			}
			var rgsCode = sCode.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
			if (rgsCode.Length != 2)
			{
				return false;
			}
			return int.TryParse(rgsCode[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) &&
				int.TryParse(rgsCode[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codeMax);
		}

		private static string ReadLineAndRemoveComment(TextReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException(nameof(reader));
			}
			var sLine = reader.ReadLine();
			if (string.IsNullOrEmpty(sLine))
			{
				return sLine;
			}
			var idxComment = sLine.IndexOf('#');
			if (idxComment >= 0)
			{
				sLine = sLine.Substring(0, idxComment);
				sLine = sLine.Trim();
			}
			return sLine.Trim();
		}

		private void ReadCustomCharData()
		{
			var customCharsFile = CustomCharsFile;
			if (File.Exists(customCharsFile))
			{
				ReadCustomCharData(customCharsFile);
			}
			else
			{
				var dir = CustomCharsDirectory;
				foreach (var sFile in Directory.GetFiles(dir, "*.xml"))
				{
					ReadCustomCharData(sFile);
				}
			}
		}

		private void ReadCustomCharData(string customCharsFile)
		{
			var xd = XDocument.Load(customCharsFile, LoadOptions.None);
			foreach (var xe in xd.Descendants("CharDef"))
			{
				var xaCode = xe.Attribute("code");
				if (string.IsNullOrEmpty(xaCode?.Value))
				{
					continue;
				}
				var xaData = xe.Attribute("data");
				if (string.IsNullOrEmpty(xaData?.Value))
				{
					continue;
				}
				int code;
				if (int.TryParse(xaCode.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) && !m_dictCustomChars.ContainsKey(code))
				{
					var spec = new PUACharacter(xaCode.Value, xaData.Value);
					m_dictCustomChars.Add(code, spec);
				}
			}
		}

		private void m_btnAdd_Click(object sender, EventArgs e)
		{
			using (var dlg = new CustomCharDlg())
			{
				dlg.PUAChar = PUACharacter.UnicodeDefault;
				dlg.Modify = false;
				dlg.SetDialogProperties(this);
				dlg.ParentDialog = this;
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				var lviNew = new PuaListItem(dlg.PUAChar);
				if (m_dictCustomChars.ContainsKey(lviNew.Code))
				{
					m_dictCustomChars[lviNew.Code] = dlg.PUAChar;
					PuaListItem lviOld = null;
					foreach (var item in m_lvCharSpecs.Items)
					{
						var pli = item as PuaListItem;
						if (pli != null && pli.Code == lviNew.Code)
						{
							lviOld = item as PuaListItem;
							break;
						}
					}

					if (lviOld != null)
					{
						m_lvCharSpecs.Items.Remove(lviOld);
					}
				}
				else
				{
					m_dictCustomChars.Add(lviNew.Code, dlg.PUAChar);
				}
				m_lvCharSpecs.Items.Add(lviNew);
				m_fDirty = true;
			}
		}

		private void m_btnEdit_Click(object sender, EventArgs e)
		{
			if (m_lvCharSpecs.SelectedItems.Count <= 0)
			{
				return;
			}
			var lvi = m_lvCharSpecs.SelectedItems[0];
			var spec = lvi.Tag as PUACharacter;
			if (spec == null)
			{
				return;
			}
			using (var dlg = new CustomCharDlg())
			{
				dlg.PUAChar = spec;
				dlg.Modify = true;
				dlg.SetDialogProperties(this);
				dlg.ParentDialog = this;
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				m_lvCharSpecs.Items.Remove(lvi);
				lvi = new PuaListItem(dlg.PUAChar);
				m_lvCharSpecs.Items.Add(lvi);
				m_fDirty = true;
			}
		}

		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			if (m_lvCharSpecs.SelectedItems.Count <= 0)
			{
				return;
			}
			var lvi = m_lvCharSpecs.SelectedItems[0] as PuaListItem;
			var spec = lvi?.Tag as PUACharacter;
			if (spec == null)
			{
				return;
			}
			var ret = MessageBox.Show($"Deleting the character definition for {spec.CodePoint} cannot be undone.  Do you want to continue?", "Warning", MessageBoxButtons.YesNo);
			if (ret != DialogResult.Yes)
			{
				return;
			}
			m_dictCustomChars.Remove(lvi.Code);
			m_lvCharSpecs.Items.Remove(lvi);
			m_fDirty = true;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(this, m_sHelpTopic);
		}

		internal PUACharacter FindCachedIcuEntry(string sCode)
		{
			try
			{
				int code;
				if (int.TryParse(sCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
				{
					PUACharacter charSpec;
					if (m_dictCustomChars.TryGetValue(code, out charSpec))
					{
						return charSpec;
					}
					if (m_dictModifiedChars.TryGetValue(code, out charSpec))
					{
						return charSpec;
					}
					charSpec = new PUACharacter(code);
					if (charSpec.RefreshFromIcu(true))
					{
						return charSpec; // known character we have no overrides for
					}
				}
			}
			catch (COMException)
			{
				// can happen if it's an invalid character
			}
			return null;
		}

		/// <summary>
		/// Is this a character for which the user has already recorded a private override?
		/// </summary>
		internal bool IsCustomChar(string sCode)
		{
			int code;
			return int.TryParse(sCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code) && m_dictCustomChars.ContainsKey(code);
		}

		private void m_lvCharSpecs_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			m_btnEdit_Click(sender, e);
		}

		/// <summary>
		/// Get the full pathname of the standard (system-wide) custom characters file.
		/// </summary>
		public static string CustomCharsFile
		{
			get
			{
				var dir = CustomCharsDirectory;
				return Path.Combine(dir, "CustomChars.xml");
			}
		}

		private static string CustomCharsDirectory
		{
			get
			{
				var icuDir = CustomIcu.DefaultDataDirectory;
				if (string.IsNullOrEmpty(icuDir))
				{
					throw new Exception("An error occurred: ICU directory not found. Registry value for ICU not set?");
				}
				// Must handle registry setting with or without final \  LT-11766.
				if (icuDir.LastIndexOf(Path.DirectorySeparatorChar) == icuDir.Length - 1)
				{
					icuDir = icuDir.Substring(0, icuDir.Length - 1);
				}
				return Path.GetDirectoryName(icuDir);   // strip the ICU specific subdirectory (FWR-2803)
			}
		}

		bool m_fBakFileCreated;

		private void m_btnSave_Click(object sender, EventArgs e)
		{
			if (m_dictCustomChars.Count == 0)
			{
				return;
			}

			var customCharsFile = CustomCharsFile;
			string oldFile = null;
			if (File.Exists(customCharsFile))
			{
				oldFile = customCharsFile + "-bak";
				if (!m_fBakFileCreated)
				{
					if (File.Exists(oldFile))
					{
						File.Delete(oldFile);
					}
					File.Move(customCharsFile, oldFile);
					m_fBakFileCreated = true;
				}
			}
			// Loop until you succeed or cancel.
			for (; ; )
			{
				try
				{
					using (var writer = new StreamWriter(customCharsFile, false, Encoding.UTF8))
					{
						writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
						writer.WriteLine("<PuaDefinitions>");
						foreach (var spec in m_dictCustomChars.Values)
						{
							writer.Write("<CharDef code=\"");
							writer.Write(spec.CodePoint);
							writer.Write("\" data=\"");
							for (var i = 0; i < spec.Data.Length; ++i)
							{
								if (!string.IsNullOrEmpty(spec.Data[i]))
								{
									writer.Write(XmlUtils.MakeSafeXmlAttribute(spec.Data[i]));
								}

								if (i + 1 < spec.Data.Length)
								{
									writer.Write(";");
								}
							}
							writer.WriteLine("\"/>");
						}

						writer.WriteLine("</PuaDefinitions>");
					}

					var inst = new PUAInstaller();
					inst.InstallPUACharacters(customCharsFile);
					if (!string.IsNullOrEmpty(oldFile) && File.Exists(oldFile))
					{
						File.Delete(oldFile);
					}
					m_fBakFileCreated = false;
					m_fDirty = false;
					return;
				}
				catch (IcuLockedException)
				{
					var res = MessageBox.Show(Properties.Resources.ksErrorOccurredInstalling, Properties.Resources.ksMsgHeader, MessageBoxButtons.RetryCancel);
					if (res == DialogResult.Cancel)
					{
						return;
					}
				}
				catch (Exception ex)
				{
					var res = MessageBox.Show(ex.Message, Properties.Resources.ksMsgHeader, MessageBoxButtons.RetryCancel);
					if (res == DialogResult.Cancel)
					{
						return;
					}
				}
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Override to check about saving changes before closing.
		/// </summary>
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			if (!m_fDirty)
			{
				return;
			}
			var res = MessageBox.Show(this, Properties.Resources.ksDoYouWantToSave, Properties.Resources.ksConfirm, MessageBoxButtons.YesNoCancel);
			switch (res)
			{
				case DialogResult.Cancel:
					e.Cancel = true;
					return;
				case DialogResult.Yes:
					m_btnSave_Click(this, e);
					break;
			}
		}
		#region IHelpTopicProvider Members

		/// <summary>
		/// Get the indicated help string.
		/// </summary>
		public string GetHelpString(string sPropName)
		{
			if (string.IsNullOrWhiteSpace(sPropName))
			{
				return "NullStringID";
			}
			if (s_helpResources == null)
			{
				s_helpResources = new ResourceManager("SIL.FieldWorks.UnicodeCharEditor.Properties.Resources", Assembly.GetExecutingAssembly());
			}
			return s_helpResources.GetString(sPropName);
		}

		/// <summary>
		/// Get the name of the help file.
		/// </summary>
		public string HelpFile => Path.Combine(FwDirectoryFinder.CodeDirectory, GetHelpString("UserHelpFile"));

		#endregion
	}
}