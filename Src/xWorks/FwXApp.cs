// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FxApp.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for XApp.
	/// </summary>
	public abstract class FwXApp : FwApp
	{
		#region Data Members
		protected FwAppArgs m_appArgs;
		#endregion // Data Members

		#region Properties

		///// <summary>
		///// Get the name of the product.
		///// </summary>
		///// <remarks>Subclasses should override this to get the proper product name.</remarks>
		//public virtual string ProductName
		//{
		//    get
		//    {
		//        CheckDisposed();
		//        return "Generic xWorks Application";
		//    }
		//}

		/// <summary>
		/// Get the pathname of the default XML configuration file,
		/// which is used if none was provided in the "-x" command line option.
		/// </summary>
		/// <remarks>Subclasses should override this to get the proper XML configuration pathname.</remarks>
		public virtual string DefaultConfigurationPathname
		{
			get
			{
				CheckDisposed();

				// TODO: See if there can be a generic config file for all of xWorks.
				// Maybe something like an editor of LangProject.
				Debug.Assert(false);
				return "";
			}
		}

		public override bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			if (sync == SyncMsg.ksyncUndoRedo || sync == SyncMsg.ksyncFullRefresh)
			{
				OnMasterRefresh(null);
				return true;
			}
			return base.Synchronize (sync);
		}

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like Clerks a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		public void OnMasterRefresh(object sender)
		{
			// TODO: This is no longer called by the Mediator, since this class
			// is no longer an xcore colleague. But, it can't be removed either,
			// since it is used by another method on this clsss. :-(
			CheckDisposed();

			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			FwXWindow activeWnd = ActiveForm as FwXWindow;

			List<FwXWindow> rgxw = new List<FwXWindow>();
			foreach (IFwMainWnd wnd in MainWindows)
			{
				FwXWindow xwnd = wnd as FwXWindow;
				if (xwnd != null)
				{
					xwnd.PrepareToRefresh();
					rgxw.Add(xwnd);
				}
			}
			if (activeWnd != null)
				rgxw.Remove(activeWnd);

			foreach (FwXWindow xwnd in rgxw)
			{
				xwnd.FinishRefresh();
				xwnd.Refresh();
			}

			// LT-3963: active window changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd != null)
			{
				// Refresh it last, so its saved settings get restored.
				activeWnd.FinishRefresh();
				activeWnd.Refresh();
				activeWnd.Activate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the XML file as stream
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Stream ConfigurationStream
		{
			get
			{
				Debug.Assert(false, "Subclasses must overrride this property.");
				return null;
			}
		}

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		/// <remarks>Subclasses should override this, or all its settings will go in "FwXapp".</remarks>
		protected virtual string SettingsKeyName
		{
			get
			{
				Debug.Assert(false, "Subclasses must overrride this property.");
				return "";
			}
		}

		#endregion // Properties

		#region Construction and Initializing

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fwManager">The FieldWorks manager for dealing with FieldWorks-level
		/// stuff.</param>
		/// <param name="helpTopicProvider">An application-specific help topic provider.</param>
		/// <param name="appArgs">The application arguments.</param>
		/// ------------------------------------------------------------------------------------
		public FwXApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider,
			FwAppArgs appArgs) : base(fwManager, helpTopicProvider)
		{
			m_appArgs = appArgs;
		}

		#endregion // Construction and Initializing

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active form. We provide this method so that we can override it
		/// in our tests where we don't show a window, and so don't have an active form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Form ActiveForm
		{
			get
			{
				if (base.ActiveForm != null)
					return base.ActiveForm;
				foreach (FwXWindow wnd in m_rgMainWindows)
				{
					if (wnd.ContainsFocus)
						return wnd;
				}
				if (m_rgMainWindows.Count > 0)
					return (Form)m_rgMainWindows[0];
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the incoming link, after the right window of the right application on the right
		/// project has been activated.
		/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		public override void HandleIncomingLink(FwLinkArgs link)
		{
			CheckDisposed();

			// Get window that uses the given DB.
			FwXWindow fwxwnd = m_rgMainWindows.Count > 0 ? (FwXWindow)m_rgMainWindows[0] : null;
			if (fwxwnd != null)
			{
				fwxwnd.Mediator.SendMessage("FollowLink", link);
				bool topmost = fwxwnd.TopMost;
				fwxwnd.TopMost = true;
				fwxwnd.TopMost = topmost;
				fwxwnd.Activate();
			}
		}

		#region ISettings implementation

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The RegistryKey for this application.
		/// </summary>
		///***********************************************************************************
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		public override RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				using (var regKey = base.SettingsKey)
				{
					return regKey.CreateSubKey(SettingsKeyName);
				}
			}
		}

		#endregion // ISettings implementation

		#region FwApp required methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main X window
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use, if needed (can be null).</param>
		/// <param name="isNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom">Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool isNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			if (isNewCache)
			{
				// TODO: Do any needed initialization here.
			}
			Stream iconStream = ApplicationIconStream;
			Debug.Assert(iconStream != null, "Couldn't find the specified application icon as a resource.");
			string configFile;
			if (m_appArgs.ConfigFile != string.Empty)
				configFile = m_appArgs.ConfigFile;
			else
			{
				configFile = DirectoryFinder.GetFWCodeFile(DefaultConfigurationPathname);
				//					configFile = (string)SettingsKey.GetValue("LatestConfigurationFile",
				//						Path.Combine(DirectoryFinder.FWCodeDirectory,
				//						DefaultConfigurationPathname));
				if (!File.Exists(configFile))
					configFile = null;
			}
			if (configFile == null) // try to load from stream
				return new FwXWindow(this, wndCopyFrom, iconStream, ConfigurationStream);

			// We pass a copy of the link information because it doesn't get used until after the following line
			// removes the information we need.
			FwXWindow result = new FwXWindow(this, wndCopyFrom, iconStream, configFile,
				m_appArgs.HasLinkInformation ? m_appArgs.CopyLinkArgs() : null, false);
			m_appArgs.ClearLinkInformation(); // Make sure the next window that is opened doesn't default to the same place
			return result;
		}

		/// <summary>
		/// override this with the name of your icon
		/// This icon file should be included in the assembly, and its "build action" should be set to "embedded resource"
		/// </summary>
		protected virtual string ApplicationIconName
		{
			get { return "app.ico"; }
		}

		/// <summary>
		/// Get a stream on the application icon.
		/// </summary>
		protected System.IO.Stream ApplicationIconStream
		{
			get
			{
				System.IO.Stream iconStream = null;
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(GetType());
				string expectedIconName = ApplicationIconName.ToLowerInvariant();
				foreach(string resourcename in assembly.GetManifestResourceNames())
				{
					if(resourcename.ToLowerInvariant().EndsWith(expectedIconName))
					{
						iconStream = assembly.GetManifestResourceStream(resourcename);
						Debug.Assert(iconStream != null, "Could not load the " + ApplicationIconName + " resource.");
						break;
					}
				}
				return iconStream;
			}
		}

		#endregion // FwApp required methods

		#region IFeedbackInfoProvider Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// E-mail address for feedback reports, kudos, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string FeedbackEmailAddress
		{
			get { return "FLEXUsage@sil.org"; }
		}
		#endregion

		#region Other methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use.</param>
		/// ------------------------------------------------------------------------------------
		public override void DoApplicationInitialization(IProgress progressDlg)
		{
			base.DoApplicationInitialization(progressDlg);
			if (FwUtils.IsTEInstalled)
				ScrReference.InitializeVersification(DirectoryFinder.TeFolder, false);

			//usage report - Unnecessary now that we are doing Google Analytics reporting
			//Improvement idea: should we do a special analytics ping for the 10 or 40 launches?
			//UsageEmailDialog.DoTrivialUsageReport(ApplicationName, SettingsKey, FeedbackEmailAddress, xWorksStrings.ThankYouForCheckingOutFlex, false, 1);
			//UsageEmailDialog.DoTrivialUsageReport(ApplicationName, SettingsKey, FeedbackEmailAddress, xWorksStrings.HaveLaunchedFLEXTenTimes, true, 10);
			//UsageEmailDialog.DoTrivialUsageReport(ApplicationName, SettingsKey, FeedbackEmailAddress, xWorksStrings.HaveLaunchedFLEXFortyTimes, true, 40);
		}
		#endregion // Other methods
	}
}
