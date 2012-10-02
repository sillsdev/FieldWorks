using System;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// SampleCitationFormTransducer can be used with the Tools:Utilities dialog
	/// It was actually built for Dennis Walters, but could be useful for someone else.
	/// </summary>
	public class SampleCitationFormTransducer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SampleCitationFormTransducer()
		{
		}

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return LexTextStrings.ksTransduceCitationForms;
			}
		}

		/// <summary>
		/// Set the UtilityDlg.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);

				m_dlg = value;
			}
		}

		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);

		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LexTextStrings.ksWhenToTransduceCitForms;
			m_dlg.WhatDescription = LexTextStrings.ksDemoOfUsingPython;
			m_dlg.RedoDescription = LexTextStrings.ksCannotUndoTransducingCitForms;
		}

		private string InvokePython(string arguments)
		{
			using (Process p = new Process())
			{
				p.StartInfo.FileName = "python";
				string dir = DirectoryFinder.GetFWCodeSubDirectory("/Language Explorer/UserScripts");
				p.StartInfo.Arguments = System.IO.Path.Combine(dir, "TransduceCitationForms.py ") + " " + arguments;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.CreateNoWindow = true;
				p.Start();
				p.WaitForExit(1000);
				string output = p.StandardOutput.ReadToEnd();
				return output;
			}
		}
		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			try
			{
				FdoCache cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
				m_dlg.ProgressBar.Maximum = cache.LanguageProject.LexDbOA.Entries.Count();
				m_dlg.ProgressBar.Step=1;
				string locale = InvokePython("-icu"); //ask the python script for the icu local
				locale = locale.Trim();
				int ws = cache.WritingSystemFactory.GetWsFromStr(locale);

				if (ws == 0)
				{
					System.Windows.Forms.MessageBox.Show(
						String.Format(LexTextStrings.ksCannotLocateWsForX, locale));
					return;
				}

				foreach (var e in cache.LanguageProject.LexDbOA.Entries)
				{
					var a = e.CitationForm;
					string src = a.VernacularDefaultWritingSystem.Text;

					string output = InvokePython("-i "+src).Trim();

					a.set_String(ws, cache.TsStrFactory.MakeString(output, ws));
					m_dlg.ProgressBar.PerformStep();
				}
			}
			catch(Exception e)
			{
				System.Windows.Forms.MessageBox.Show(
					String.Format(LexTextStrings.ksErrorMsgWithStackTrace, e.Message, e.StackTrace));
			}
		}
		#endregion IUtility implementation
	}
}
