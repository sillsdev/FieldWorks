using System;
using System.Collections.Specialized;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;

namespace FwNantAddin2
{
	public partial class Connect : IDTExtensibility2
	{
		private CommandBar m_cmdBar;

		private CmdHandler m_nantCommands;
		private CommandBarButton m_btnCancel;
		private CommandBarButton m_btnClean;
		private CommandBarButton m_btnTest;
		private CommandBarButton m_btnForceTests;
		private CommandBarButton m_btnNodep;
		private CommandBarButton m_btnEnableAddin;
		private CommandBarButton m_btnStartBuild;

		public static object missing = System.Reflection.Missing.Value;

		public const string kCmdBarName = "FwNantAddIn2";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeleteToolbar()
		{
			try
			{
				if (m_cmdBar != null)
				{
					DTE.Commands.RemoveCommandBar(m_cmdBar);
					m_cmdBar = null;
				}
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Got exception deleting toolbar: " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddToolbar()
		{
			try
			{
				CommandBars commandBars = DTE.CommandBars as CommandBars;
				try
				{
					m_cmdBar = commandBars[kCmdBarName];
				}
				catch (ArgumentException)
				{ // if we get an exception the toolbar doesn't exist, so just ignore it
				}

				if (m_cmdBar == null)
					m_cmdBar = (CommandBar)DTE.Commands.AddCommandBar(kCmdBarName, vsCommandBarType.vsCommandBarTypeToolbar, null, 0);
				m_cmdBar.Visible = false;

				m_btnEnableAddin = AddButton("Enable addin", "Resources.OnOff.bmp",
					"Resources.OnOff_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnEnableAddin), true,
					"Enable/Disable addin");
				m_nantCommands.EnableAddin = m_nantCommands.EnableAddin; // this sets the button

				m_btnCancel = AddButton("Cancel running build", "Resources.CancelBuild.bmp",
					"Resources.CancelBuild_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnCancelBuild), false,
					"Cancel build");
				m_btnCancel.Enabled = false;

				m_btnClean = AddButton("Clean", "Resources.cleanbuild.bmp",
					"Resources.cleanbuild_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnCleanBuild), true,
					"Clean build");
				//"Sets a flag so that the next build will do a clean build, that is erase all files produced by the build process");
				m_nantCommands.Clean = m_nantCommands.Clean; // this sets the button

				m_btnTest = AddButton("Tests", "Resources.testbuild.bmp",
					"Resources.testbuild_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnEnableTests), true,
					"Run tests");
				//"Sets a flag so that the next build will run the tests when compiling");
				m_nantCommands.Test = m_nantCommands.Test; // this sets the button

				m_btnForceTests = AddButton("Force Tests", "Resources.forcetests.bmp",
					"Resources.forcetests_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnForceTests), true,
					"Sets forcetests flag");
				//"Forces the tests to run even if nothing changed");
				m_nantCommands.ForceTests = m_nantCommands.ForceTests; // this sets the button

				m_btnNodep = AddButton("-nodep", "Resources.nodep.bmp",
					"Resources.nodep_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnNoDep), true,
					"Build without dependencies");
				// "Sets a flag so that the next build will build dependencies");
				m_nantCommands.NoDep = m_nantCommands.NoDep; // this sets the button

				CommandBarComboBox comboBox;
				m_cmdBar.Controls.Add(MsoControlType.msoControlComboBox, missing, missing,
					missing, false);
				comboBox = (CommandBarComboBox)m_cmdBar.Controls[m_cmdBar.Controls.Count];
				comboBox.DropDownLines = 12;
				comboBox.DropDownWidth = 200;
				comboBox.Style = MsoComboStyle.msoComboNormal;
				comboBox.Enabled = true;
				comboBox.Width = 200;
				m_nantCommands.m_cmbBuild = comboBox;

				m_btnStartBuild = AddButton("StartBuild", "Resources.StartBuild.bmp",
					"Resources.StartBuild_mask.bmp",
					new _CommandBarButtonEvents_ClickEventHandler(m_nantCommands.OnStartBuild), true,
					"Start a build");

				LoadToolbarSettings();
			}
			catch (Exception e)
			{
#if DEBUG
				//				m_nantCommands.OutputBuildDebug.WriteLine("Got exception in AddToolbar:" + e.Message);
				System.Diagnostics.Debug.WriteLine("Got exception in AddToolbar:" + e.Message);
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the button.
		/// </summary>
		/// <param name="caption">The caption.</param>
		/// <param name="bitmapName">Name of the bitmap.</param>
		/// <param name="maskName">Name of the mask.</param>
		/// <param name="eventHandler">The event handler.</param>
		/// <param name="fEnabled">if set to <c>true</c> [f enabled].</param>
		/// <param name="tooltipText">The tooltip text.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected CommandBarButton AddButton(string caption, string bitmapName,
			string maskName, _CommandBarButtonEvents_ClickEventHandler eventHandler,
			bool fEnabled, string tooltipText)
		{
			CommandBarButton btn;
			m_cmdBar.Controls.Add(MsoControlType.msoControlButton, missing, missing,
				missing, false);
			btn = (CommandBarButton)m_cmdBar.Controls[m_cmdBar.Controls.Count];
			ImageHelper.SetPicture(btn, bitmapName, maskName);
			btn.Caption = caption;
			btn.Click += eventHandler;
			btn.Enabled = fEnabled;
			if (tooltipText != null && tooltipText != string.Empty)
				btn.TooltipText = tooltipText;

			return btn;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveToolbar()
		{
			try
			{
				SaveToolbarSettings();
				if (m_nantCommands != null)
				{
					if (m_btnEnableAddin != null)
					{
						m_btnEnableAddin.Delete(true);
						m_btnEnableAddin = null;
					}
					if (m_btnCancel != null)
					{
						m_btnCancel.Delete(true);
						m_btnCancel = null;
					}
					if (m_btnClean != null)
					{
						m_btnClean.Delete(true);
						m_btnClean = null;
					}
					if (m_btnTest != null)
					{
						m_btnTest.Delete(true);
						m_btnTest = null;
					}
					if (m_btnForceTests != null)
					{
						m_btnForceTests.Delete(true);
						m_btnForceTests = null;
					}
					if (m_btnNodep != null)
					{
						m_btnNodep.Delete(true);
						m_btnNodep = null;
					}
					if (m_nantCommands.m_cmbBuild != null)
					{
						m_nantCommands.m_cmbBuild.Delete(true);
						m_nantCommands.m_cmbBuild = null;
					}
					if (m_btnStartBuild != null)
					{
						m_btnStartBuild.Delete(true);
						m_btnStartBuild = null;
					}
					m_nantCommands.Dispose();
				}
				m_nantCommands = null;
				DeleteToolbar();
			}
			catch (Exception e)
			{
#if DEBUG
				//				m_nantCommands.OutputBuildDebug.WriteLine("Got exception in RemoveToolbar:" + e.Message);
				System.Diagnostics.Debug.WriteLine("Got exception in RemoveToolbar:" + e.Message);
#endif
			}
			m_cmdBar = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the toolbar settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SaveToolbarSettings()
		{
			Settings settings = Settings.Default;
			settings.ToolbarPos = new System.Drawing.Point(m_cmdBar.Left, m_cmdBar.Top);
			settings.ToolbarVisible = m_cmdBar.Visible;
			settings.ToolbarState = (int)m_cmdBar.Position;
			settings.ToolbarRowIndex = m_cmdBar.RowIndex;
			settings.AddinEnabled = m_nantCommands.EnableAddin;

			if (m_nantCommands.m_cmbBuild != null)
			{
				CommandBarComboBox cbox = m_nantCommands.m_cmbBuild;
				StringCollection commands = new StringCollection();
				for (int i = cbox.ListCount; i > 0; i--)
					commands.Add(cbox.get_List(i));

				settings.BuildCommands = commands;
			}
			settings.Save();
		}

		protected void LoadToolbarSettings()
		{
			Settings settings = Settings.Default;
			m_cmdBar.Position = (MsoBarPosition)settings.ToolbarState;
			m_cmdBar.Left = settings.ToolbarPos.X;
			m_cmdBar.Top = settings.ToolbarPos.Y;
			m_cmdBar.Visible = settings.ToolbarVisible;
			m_cmdBar.RowIndex = settings.ToolbarRowIndex;
			m_nantCommands.EnableAddin = settings.AddinEnabled;

			if (m_nantCommands.m_cmbBuild != null)
			{
				if (settings.BuildCommands.Count != 0)
				{
					foreach (string command in settings.BuildCommands)
					{
						m_nantCommands.AddItemToCombo(command);
					}
				}
				else
				{	// no entries stored, so put in default list
					m_nantCommands.AddItemToCombo("test all");
					m_nantCommands.AddItemToCombo("remakefw");
					m_nantCommands.AddItemToCombo("restoreTLP");
					m_nantCommands.AddItemToCombo("build TeExe");
					m_nantCommands.AddItemToCombo("register build all");
				}
			}
		}
	}
}
