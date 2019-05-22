using System.Collections.Generic;

namespace IoPS
{
	public class PSExecutionParameters
	{
		public string ScriptName { get; set; }
		public string ExecutionPolicy { get; set; }
		public Dictionary<string, string> Parameters { get; set; }
	}
}