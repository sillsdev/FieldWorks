using System.ServiceProcess;
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

			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[]
			{
				new FwRemoteDatabaseConnectorService()
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
