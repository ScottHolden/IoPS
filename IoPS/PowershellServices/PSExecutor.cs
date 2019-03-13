using Microsoft.PowerShell;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.RegularExpressions;

namespace IoPS
{
	public class PSExecutor
	{
		private static readonly Regex VaildPSScriptName = new Regex(@"^[a-z0-9\-.]+\.ps1$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

		private readonly string _scriptPath;
		private readonly ILogger _logger;

		public PSExecutor(ConfigurationService config, ILogger logger)
		{
			_scriptPath = config.GetSetting("scriptpath", Path.GetDirectoryName(typeof(PSExecutor).Assembly.Location));
			_logger = logger.ForContext<PSExecutor>();
		}

		public string[] ListAvailableScripts()
		{
			_logger.Information("Listing available scripts");

			if (!Directory.Exists(_scriptPath))
			{
				_logger.Error("Script path {Path} does not exist", _scriptPath);

				return new[] { "Script directory does not exist" };
			}

			string[] scripts = Directory.EnumerateFiles(_scriptPath, "*.ps1")
					.Select(Path.GetFileName)
					.Where(x => VaildPSScriptName.IsMatch(x))
					.ToArray();

			_logger.Information("Found {ScriptCount} scripts", scripts.Length);

			return scripts;
		}

		public PSExecutionResults ExecuteScript(PSExecutionParameters parameters)
		{
			_logger.Information("Attemping to execute script");

			if (!parameters.ScriptName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
			{
				parameters.ScriptName += ".ps1";
			}

			if (!VaildPSScriptName.IsMatch(parameters.ScriptName))
			{
				_logger.Error("Script name was not vaild");

				return new PSExecutionResults
				{
					Errors = new[] { "Script name is invalid" },
					Completed = false
				};
			}

			string safeFullPath = Directory.EnumerateFiles(_scriptPath, "*.ps1")
											.Where(x => Path.GetFileName(x).Equals(parameters.ScriptName, StringComparison.OrdinalIgnoreCase))
											.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(safeFullPath))
			{
				_logger.Error("Script file was not found");

				return new PSExecutionResults
				{
					Errors = new[] { "Script file does not exist" },
					Completed = false
				};
			}

			_logger.Information("Setting up pipeline for {ScriptPath}", safeFullPath);

			RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();

			using (Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration))
			using (RunspaceInvoke runspaceInvoke = new RunspaceInvoke(runspace))
				try
				{
					runspace.Open();

					_logger.Information("Setting Execution Policy for process");

					runspaceInvoke.Invoke("Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned");

					using (Pipeline pipeline = runspace.CreatePipeline())
					{
						RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);

						_logger.Information("Building script command");

						Command scriptCommand = new Command(safeFullPath);

						if (parameters.Parameters != null && parameters.Parameters.Count > 0)
							foreach (KeyValuePair<string, string> p in parameters.Parameters)
							{
								scriptCommand.Parameters.Add(new CommandParameter(p.Key, p.Value));
							}

						pipeline.Commands.Add(scriptCommand);

						try
						{
							_logger.Information("Invoking Script");

							Collection<PSObject> results = pipeline.Invoke();

							string[] errors = null;

							if (pipeline.Error.Count > 0)
							{
								Collection<ErrorRecord> pipelineErrors = pipeline.Error.Read() as Collection<ErrorRecord>;

								errors = pipelineErrors.Select(x => x.ToString()).ToArray();
							}

							string[] output = results.Select(x => x.ToString()).ToArray();

							_logger.Information("Script completed, {OutputCount} outputs, {ErrorCount} errors", output.Length, errors?.Length ?? 0);

							return new PSExecutionResults
							{
								Output = output,
								Errors = errors,
								Completed = true
							};
						}
						catch (RuntimeException re)
						{
							_logger.Error(re, "Runtime exception in pipeline, {ExceptionMessage}", re.Message);

							return new PSExecutionResults
							{
								Errors = new[] { re.Message },
								Completed = false
							};
						}
					}
				}
				finally
				{
					runspace.Close();
				}
		}
	}
}