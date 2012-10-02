using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;                // for Assembly
using System.Runtime.InteropServices;   // for RegistrationServicess

namespace SILConvertersOffice
{
	[RunInstaller(true)]
	public partial class InstallerRegistrationActions : Installer
	{
		public InstallerRegistrationActions()
		{
			InitializeComponent();
		}

		enum InstallState
		{
			InstallStateNone,
			InstallStateInstalling,
			InstallStateUninstalling
		};

		InstallState m_eInstallingState = InstallState.InstallStateNone;

		public static void RegisterSelf()
		{
			RegistrationServices aRS = new RegistrationServices();
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			aRS.RegisterAssembly(thisAssembly, AssemblyRegistrationFlags.SetCodeBase);
		}

		public static void UnregisterSelf()
		{
			RegistrationServices aRS = new RegistrationServices();
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			if (thisAssembly != null)
				aRS.UnregisterAssembly(thisAssembly);
		}

		public override void Install(System.Collections.IDictionary stateSaver)
		{
			base.Install(stateSaver);
			m_eInstallingState = InstallState.InstallStateInstalling;
		}

		public override void Uninstall(System.Collections.IDictionary savedState)
		{
			m_eInstallingState = InstallState.InstallStateUninstalling;
			UnregisterSelf();
			base.Uninstall(savedState);
		}

		public override void Commit(System.Collections.IDictionary savedState)
		{
			base.Commit(savedState);
			RegisterSelf();
		}

		public override void Rollback(System.Collections.IDictionary savedState)
		{
			if (m_eInstallingState == InstallState.InstallStateUninstalling)
			{
				RegisterSelf();
			}
			else
			{
				UnregisterSelf();
			}

			base.Rollback(savedState);
		}
	}
}