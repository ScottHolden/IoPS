namespace IoPS
{
	public interface IPSFileService
	{
		string[] ListAvailableScripts();

		bool IsVaildScriptName(string scriptName);

		(string path, string errors) GetScript(string scriptName);
	}
}