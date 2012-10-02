using System;
using System.Collections.Generic;
using System.Text;
using LibronixDLS;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

namespace SIL.FieldWorks.TE.LibronixLinker
{
	/// <summary>
	/// Class to manage saving and restoring libronix workspace.
	/// </summary>
	public class LibronixWorkspaceManager
	{
		/// <summary>
		/// If Libronix is running, save its workspace in the specified file.
		/// <param name="path"></param>
		/// </summary>
		public static void SaveWorkspace(string path)
		{
			try
			{
				// If Libronix isn't running, we'll get an exception here
				object libApp = Marshal.GetActiveObject("LibronixDLS.LbxApplication");
				if (libApp == null)
					return;

				LbxApplication libronixApp = libApp as LbxApplication;
				object document = libronixApp.MSXML.CreateDocument(0);
				//MSXML2.DOMDocument40 doc = new MSXML2.DOMDocument40();
				//doc.
				libronixApp.SaveWorkspace(document, "");
				MSXML2.DOMDocument doc = document as MSXML2.DOMDocument;
				doc.save(path);
				//Type docType = document.GetType();
				//MethodInfo info = docType.GetMethod("save");
				//if (info != null)
				//    info.Invoke(document, new object[] {path});
			}
			catch (COMException)
			{
				return;
			}
		}

		/// <summary>
		/// If Libronix is NOT running, start it up, and restore the specified workspace.
		/// </summary>
		/// <param name="path"></param>
		public static void RestoreIfNotRunning(string path)
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
			worker.RunWorkerAsync(path);
		}

		static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
				MessageBox.Show(e.Error.Message);
		}

		static void DoTheRestore(string path)
		{
			LbxApplication libronixApp = null;
			try
			{
				// If Libronix isn't running, we'll get an exception here
				object libApp = Marshal.GetActiveObject("LibronixDLS.LbxApplication");
				return; // It IS running; don't disturb it.
			}
			catch (COMException e)
			{
				if ((uint)e.ErrorCode == 0x800401E3) // MK_E_UNAVAILABLE
				{
					// try to start
					libronixApp = new LbxApplicationClass();
				}
			}
			if (libronixApp == null) // can't start, or not installed.
				return;
			try
			{
				// Try to load workspace.
				if (!File.Exists(path))
				{
					libronixApp.Visible = true; //let them see it, anyway.
					return;
				}
				object document = libronixApp.MSXML.CreateDocument(0);
				MSXML2.DOMDocument doc = document as MSXML2.DOMDocument;
				doc.load(path);

				//Type docType = document.GetType();
				//MethodInfo info = docType.GetMethod("Save");
				//if (info == null)
				//{
				//    ReportLoadProblem();
				//    return;
				//}
				//info.Invoke(document, new object[] { path });
				libronixApp.LoadWorkspace(document, "", DlsSaveChanges.dlsPromptToSaveChanges);
				libronixApp.Visible = true; //only after we reload the workspace, to save flashing.
			}
			catch (Exception)
			{
				libronixApp.Visible = true; //let them see it, anyway.
				ReportLoadProblem();
			}
		}

		static void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			DoTheRestore(e.Argument as string);
		}

		private static void ReportLoadProblem()
		{
			throw new Exception("Unable to reload Libronix workspace");
		}
	}
}
