using System.Threading.Tasks;

namespace IoPS
{
	public interface IIoTDevice
	{
		Task ConnectAsync();

		Task ShutdownAsync();
	}
}