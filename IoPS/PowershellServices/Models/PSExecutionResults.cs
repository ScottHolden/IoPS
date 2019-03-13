using Newtonsoft.Json;

namespace IoPS
{
	public class PSExecutionResults
	{
		public bool Completed { get; set; }
		public string[] Output { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string[] Errors { get; set; }
	}
}