using System.Runtime.InteropServices;

namespace SrvCommon
{
	public static class Win32Natives
	{
		public enum KeyActionState
		{
			Down = 0x01,
			Up = 0x02,
			Press = 0x03
		}
		//keybd_event(VirtualKey, 0, (int)KeyEvents.KeyUp, silent);
		[DllImport(NativeMethods.WIN_DLL)]
		public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
	}
}
