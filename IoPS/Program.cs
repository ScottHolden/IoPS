using Autofac;
using Serilog;
using System;
using System.Linq;
using System.ServiceProcess;

namespace IoPS
{
	public static class Program
	{
		private static readonly string[] InstallArguments = new[] { "--install", "-i" };
		private static readonly string[] UninstallArguments = new[] { "--uninstall", "-u" };

		public static int Main(string[] args)
		{
			bool install = args.Intersect(InstallArguments).Any();
			bool uninstall = args.Intersect(UninstallArguments).Any();

			if (install && uninstall)
			{
				Console.WriteLine("Can't install and uninstall at the same time");
				return 1;
			}
			else if (install || uninstall)
			{
				return IoTServiceInstaller.ModifyService(uninstall, args);
			}

			using (IContainer container = BuildServiceContainer())
			{
				IoTService service = container.Resolve<IoTService>();

				if (Environment.UserInteractive)
				{
					service.RunAsConsole(args);
				}
				else
				{
					ServiceBase.Run(service);
				}
			}

			return 0;
		}

		private static IContainer BuildServiceContainer()
		{
			ContainerBuilder builder = new ContainerBuilder();

			builder.RegisterInstance(BuildLogger());

			builder.RegisterType<ConfigurationService>();
			builder.RegisterType<PSExecutor>();
			builder.RegisterType<IoTDevice>();
			builder.RegisterType<IoTService>();

			return builder.Build();
		}

		private static ILogger BuildLogger()
		{
			return new LoggerConfiguration()
					.Enrich.WithAssemblyName()
					.Enrich.WithAssemblyVersion()
					.Enrich.WithMachineName()
					.Enrich.WithProcessName()
					.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
					.CreateLogger();
		}
	}
}