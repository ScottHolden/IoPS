using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace IoPS
{
	[RunInstaller(true)]
	public class IoTServiceInstaller : Installer
	{
		private ServiceProcessInstaller _processInstaller;
		private ServiceInstaller _serviceInstaller;

		public IoTServiceInstaller()
		{
			_processInstaller = new ServiceProcessInstaller();
			_serviceInstaller = new ServiceInstaller();

			_processInstaller.Account = ServiceAccount.LocalSystem;

			_serviceInstaller.StartType = ServiceStartMode.Automatic;
			_serviceInstaller.ServiceName = IoTService.FullServiceName;

			Installers.Add(_serviceInstaller);
			Installers.Add(_processInstaller);
		}

		public static int ModifyService(bool uninstall, string[] args)
		{
			Hashtable state = new Hashtable();
			try
			{
				int result = ModifyService(uninstall, args, state);

				Console.WriteLine("Service installed");

				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);

				Console.WriteLine("Failed to install service, are you running as administrator?");

				return 1;
			}
		}

		private static int ModifyService(bool uninstall, string[] args, IDictionary state)
		{
			using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
			{
				inst.UseNewContext = true;

				try
				{
					if (uninstall)
					{
						inst.Uninstall(state);
					}
					else
					{
						inst.Install(state);
						inst.Commit(state);
					}

					return 0;
				}
				catch
				{
					inst.Rollback(state);

					throw;
				}
			}
		}
	}
}