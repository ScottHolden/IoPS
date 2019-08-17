using Serilog;
using System;
using System.ServiceProcess;

namespace IoPS
{
	public class IoTService : ServiceBase
	{
		public static readonly string FullServiceName = "IoT-PS-Service";

		private readonly ILogger _logger;
		private readonly IIoTDevice _iotDevice;

		public IoTService(IIoTDevice iotDevice, ILogger logger)
		{
			_iotDevice = iotDevice;
			_logger = logger.ForContext<IoTService>();

			ServiceName = FullServiceName;
			EventLog.Log = "Application";

			CanStop = true;
			AutoLog = true;
		}

		protected override void OnStart(string[] args)
		{
			_logger.Information("Starting service...");

			_iotDevice.ConnectAsync().Wait();
		}

		protected override void OnStop()
		{
			_logger.Information("Stopping service...");

			_iotDevice.ShutdownAsync().Wait();
		}

		public void RunAsConsole(string[] args)
		{
			OnStart(args);

			Console.WriteLine("Press any key to stop...");
			Console.ReadLine();

			OnStop();
		}
	}
}