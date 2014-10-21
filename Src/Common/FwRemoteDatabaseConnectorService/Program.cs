using System.ServiceProcess;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace FwRemoteDatabaseConnectorService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			// We need FieldWorks here to get the correct registry key HKLM\Software\SIL\FieldWorks.
			// The default without this would be HKLM\Software\SIL\SIL FieldWorks,
			// which breaks FwRemoteDatabaseConnectorService.exe.
			RegistryHelper.ProductName = "FieldWorks";

			ClientServerServices.SetCurrentToDb4OBackend(new SilentFdoUI(new SingleThreadedSynchronizeInvoke()),
				FwDirectoryFinder.FdoDirectories);

			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[]
			{
				new FwRemoteDatabaseConnectorService()
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
