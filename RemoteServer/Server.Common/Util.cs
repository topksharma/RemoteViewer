using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace SrvCommon
{
	public static class Util
	{
		public const int SERVER_PORT = 8989;
		public static string GetHostName()
		{
			try
			{
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey("Ident", true);
				string name = regKey.GetValue("Name", "").ToString();
				regKey.Close();
				return name;
			}
			catch (Exception exception)
			{
				//Logger.LogError("GetHostName-" + exception);
			}
			return "";
		}
		public static string GetIPAddress()
		{
			StringBuilder ipAddress = new StringBuilder();
			try
			{
				IPHostEntry ipEntry = Dns.GetHostEntry(GetHostName());
				IPAddress[] addr = ipEntry.AddressList;

				if (addr != null && addr.Length > 0)
				{
					for (int i = 0; i < addr.Length; i++)
					{
						if (addr[i].AddressFamily == AddressFamily.InterNetwork)
						{
							if (ipAddress.Length > 0)
							{
								ipAddress.Append(",");
							}
							ipAddress.Append(addr[i]);
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				//Logger.LogError("GetIPAddress-" + ex);
			}
			return ipAddress.ToString();
		}
	}
}
