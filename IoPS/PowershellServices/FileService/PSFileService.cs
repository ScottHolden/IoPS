using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;

namespace IoPS
{
	public class PSFileService : IPSFileService
	{
		private static readonly Regex VaildPSScriptName = new Regex(@"^[a-z0-9\-.]+\.ps1$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private readonly ILogger _logger;
		private readonly string[] _scriptPaths;

		public PSFileService(ConfigurationService config, ILogger logger)
		{
			_logger = logger.ForContext<PSFileService>();

			string scriptPath = config.GetSetting("scriptpath", Path.GetDirectoryName(typeof(PSExecutor).Assembly.Location));

			_scriptPaths = scriptPath.Split(',', ';')
										.Where(x => !string.IsNullOrWhiteSpace(x))
										.Select(x => x.Trim())
										.ToArray();

			_logger.Information("{PathCount} script paths configured", _scriptPaths.Length);
		}

		public bool IsVaildScriptName(string scriptName) => VaildPSScriptName.IsMatch(scriptName);

		public (string path, string errors) GetScript(string scriptName)
		{
			if (!scriptName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
			{
				scriptName += ".ps1";
			}

			if (!IsVaildScriptName(scriptName))
			{
				return (null, "Script name is invalid");
			}

			List<string> validScriptPaths = GetValidScriptPaths();

			if (validScriptPaths == null || validScriptPaths.Count < 1)
			{
				return (null, "No script directories exist");
			}

			string safeFullPath = validScriptPaths.SelectMany(x => Directory.EnumerateFiles(x, "*.ps1"))
											.Where(x => Path.GetFileName(x).Equals(scriptName, StringComparison.OrdinalIgnoreCase))
											.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(safeFullPath) || !File.Exists(safeFullPath))
			{
				return (null, "Script file does not exist");
			}

			return (safeFullPath, null);
		}

		public string[] ListAvailableScripts()
		{
			_logger.Information("Listing available scripts");

			List<string> validScriptPaths = GetValidScriptPaths();

			if (validScriptPaths == null || validScriptPaths.Count < 1)
			{
				_logger.Error("No script directories exist");

				return new string[0];
			}

			string[] scripts = validScriptPaths.SelectMany(x => Directory.EnumerateFiles(x, "*.ps1"))
					.Select(Path.GetFileName)
					.Where(x => VaildPSScriptName.IsMatch(x))
					.ToArray();

			foreach (string duplicateScript in scripts.Select(Path.GetFileName)
														.GroupBy(x => x)
														.Where(x => x.Count() > 1)
														.Select(x => x.Key))
			{
				_logger.Warning("Multiple scripts found with name {ScriptName}", duplicateScript);
			}

			_logger.Information("Found {ScriptCount} scripts", scripts.Length);

			return scripts;
		}

		private List<string> GetValidScriptPaths()
		{
			List<string> validScriptPaths = new List<string>();
			foreach (string path in _scriptPaths)
			{
				if (Directory.Exists(path))
				{
					validScriptPaths.Add(path);
				}
				else
				{
					_logger.Warning("Script path {Path} does not exist", path);
				}
			}
			return validScriptPaths;
		}
	}
}