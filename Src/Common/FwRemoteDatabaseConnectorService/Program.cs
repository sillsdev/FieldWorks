using System.ServiceProcess;

namespace FwRemoteDatabaseConnectorService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[]
			{
				new FwRemoteDatabaseConnectorService()
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
