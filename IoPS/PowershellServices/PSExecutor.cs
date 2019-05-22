using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Serilog;

namespace IoPS
{
	public class PSExecutor
	{
		private readonly ILogger _logger;
		private readonly IPSFileService _fileService;

		public PSExecutor(IPSFileService fileService, ILogger logger)
		{
			_logger = logger.ForContext<PSExecutor>();
			_fileService = fileService;
		}

		public string[] ListAvailableScripts()
		{
			string[] scripts = _fileService.ListAvailableScripts();

			if (scripts == null || scripts.Length < 1)
			{
				return new[] { "No scripts exist, or directory is missing" };
			}

			return scripts;
		}

		public PSExecutionResults ExecuteScript(PSExecutionParameters parameters)
		{
			_logger.Information("Attemping to execute script");

			(string safeFullPath, string pathErrors) = _fileService.GetScript(parameters.ScriptName);

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