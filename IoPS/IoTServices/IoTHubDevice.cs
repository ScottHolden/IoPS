using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Serilog;

namespace IoPS
{
	public class IoTHubDevice : IDisposable, IIoTDevice
	{
		private const string StatusChangeMessage = "IoT Hub {status}, {reason}";

		private readonly DeviceClient _deviceClient;
		private readonly ILogger _logger;
		private readonly PSExecutor _psExecutor;

		private readonly Dictionary<string, MethodCallback> _handlers;

		public IoTHubDevice(PSExecutor psExecutor, ConfigurationService config, ILogger logger)
		{
			_logger = logger.ForContext<IoTHubDevice>();
			_psExecutor = psExecutor;

			_handlers = new Dictionary<string, MethodCallback>
			{
				{ nameof(ListScripts), ListScripts },
				{ nameof(ExecuteScript), ExecuteScriptAsyncWrapper }
			};

			string connectionString = config.GetSetting("iothub");

			_deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

			IRetryPolicy retryPolicy = new ExponentialBackoff(int.MaxValue,
				TimeSpan.FromSeconds(1),
				TimeSpan.FromSeconds(30),
				TimeSpan.FromSeconds(1));

			_deviceClient.SetRetryPolicy(retryPolicy);
		}

		public async Task ConnectAsync()
		{
			_logger.Information("Setting up system handlers");

			_deviceClient.SetConnectionStatusChangesHandler(StatusChangeHandler);

			_logger.Information("Setting up {HandlerCount} method handlers", _handlers.Count);

			foreach (KeyValuePair<string, MethodCallback> handler in _handlers)
			{
				await DoubleCaseRegisterHandler(handler.Key, handler.Value);
			}

			_logger.Information("Ensuring connection to IoT Hub is open");

			await _deviceClient.OpenAsync();
		}

		private async Task DoubleCaseRegisterHandler(string name, MethodCallback callback)
		{
			string lowerName = name.ToLower();

			_logger.Information("Registering method handler {HandlerName} and {HandlerLowerName}", name, lowerName);

			await _deviceClient.SetMethodHandlerAsync(name, callback, null);
			await _deviceClient.SetMethodHandlerAsync(lowerName, callback, null);
		}

		public async Task ShutdownAsync()
		{
			await _deviceClient.CloseAsync();
		}

		private Task<MethodResponse> WrapWithTaskLogging(MethodRequest methodRequest, Func<MethodRequest, MethodResponse> func)
		{
			_logger.Information("{MethodName} was called via Direct Method", methodRequest.Name);

			Stopwatch sw = Stopwatch.StartNew();

			MethodResponse response = func(methodRequest);

			sw.Stop();

			_logger.Information("{MethodName} completed with status {Status} in {Elapsed} ms.", methodRequest.Name, response.Status, sw.ElapsedMilliseconds);

			return Task.FromResult(response);
		}

		private Task<MethodResponse> ListScripts(MethodRequest methodRequest, object userContext) =>
			WrapWithTaskLogging(methodRequest, request =>
			{
				string[] scripts = _psExecutor.ListAvailableScripts();
				return BuildJsonMethodResponse(200, scripts);
			});

		private Task<MethodResponse> ExecuteScriptAsyncWrapper(MethodRequest methodRequest, object userContext)
			=> WrapWithTaskLogging(methodRequest, ExecuteScript);

		private MethodResponse ExecuteScript(MethodRequest methodRequest)
		{
			PSExecutionParameters parameters;

			try
			{
				parameters = JsonConvert.DeserializeObject<PSExecutionParameters>(methodRequest.DataAsJson);

				PSExecutionResults results = _psExecutor.ExecuteScript(parameters);

				return BuildJsonMethodResponse(200, results);
			}
			catch (JsonException e)
			{
				_logger.Error(e, "Unable to deserialise payload from direct method, {ExceptionMessage}", e.Message);

				return BuildJsonMethodResponse(400, "Bad Json Provided");
			}
			catch (Exception e)
			{
				_logger.Error(e, "Unknown exception, {ExceptionMessage}", e.Message);

				return BuildJsonMethodResponse(500, "Unknown Exception");
			}
		}

		private MethodResponse BuildJsonMethodResponse(int status, object o)
		{
			string json = JsonConvert.SerializeObject(o);

			byte[] bytes = Encoding.UTF8.GetBytes(json);

			return new MethodResponse(bytes, status);
		}

		private void StatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
		{
			if (status != ConnectionStatus.Connected)
			{
				_logger.Warning(StatusChangeMessage, status, reason);
			}
			else
			{
				_logger.Information(StatusChangeMessage, status, reason);
			}

			if (status == ConnectionStatus.Disconnected &&
				(reason == ConnectionStatusChangeReason.Retry_Expired ||
				 reason == ConnectionStatusChangeReason.No_Network ||
				 reason == ConnectionStatusChangeReason.Communication_Error ||
				 reason == ConnectionStatusChangeReason.Client_Close
				))
			{
				// Hard retry
				_logger.Information("Attempting to reopen");

				_deviceClient.OpenAsync().Wait();
			}
		}

		public void Dispose()
		{
			_deviceClient.Dispose();
		}
	}
}