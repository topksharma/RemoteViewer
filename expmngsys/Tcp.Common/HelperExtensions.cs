using System.IO;
using System.Linq;
using System.Reflection;

namespace Tcp.Common
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Net.Mail;
	using System.Net.Sockets;
	using System.Text;
	using System.Xml.Linq;

	public static class HelperExtensions
	{
		public static Int32 ToInt32(this string strValue)
		{
			if (!string.IsNullOrEmpty(strValue))
			{
				return Convert.ToInt32(strValue);
			}
			return 0;
		}

		public static Boolean ToBoolean(this string strValue)
		{
			if (!string.IsNullOrEmpty(strValue))
			{
				return Convert.ToBoolean(strValue);
			}
			return false;
		}
		public static Int64 ToInt64(this string strValue)
		{
			if (!string.IsNullOrEmpty(strValue))
			{
				return Convert.ToInt64(strValue);
			}
			return 0;
		}
		public static Double ToDouble(this string strValue)
		{
			if (!string.IsNullOrEmpty(strValue))
			{
				return Convert.ToDouble(strValue);
			}
			return 0.0;
		}

		public static Double ToSingle(this string strValue)
		{
			if (!string.IsNullOrEmpty(strValue))
			{
				return Convert.ToSingle(strValue);
			}
			return 0.0f;
		}

		public static bool CreateDirectory(this string str)
		{
			try
			{
				if (!Directory.Exists(str))
				{
					Directory.CreateDirectory(str);
					return true;
				}
			}
			catch { }
			return false;
		}
		public static object ToEnum(this string strValue, Type enumType)
		{
			try
			{
				if (Enum.IsDefined(enumType, strValue))
				{
					object obj = Enum.Parse(enumType, strValue);
					return obj;
				}
			}
			catch (Exception)
			{

			}
			return Activator.CreateInstance(enumType);
		}
		public static string GetAttributeValue(this XElement element, string attributeName)
		{
			XAttribute xAttribute = element.Attribute(attributeName);
			if (xAttribute != null)
			{
				return xAttribute.Value;
			}
			return string.Empty;
		}

		public static void AddAttribute(this XElement element, string name, object value)
		{
			if (value == null)
			{
				value = "";
			}
			element.Add(new XAttribute(name, value));
		}

		public static XElement ToXml(this object obj)
		{
			if (obj != null)
			{
				Type objType = obj.GetType();
				XElement xElement = new XElement(objType.Name);
				PropertyInfo[] propertyInfos = objType.GetProperties();

				for (int i = 0; i < propertyInfos.Length; i++)
				{
					Type propType = propertyInfos[i].PropertyType;
					if (propType.IsArray || propType.IsGenericType)
					{

					}
					else
					{
						xElement.AddAttribute(propertyInfos[i].Name, propertyInfos[i].GetValue(obj));
					}
				}
				return xElement;
			}
			return null;
		}

		public static void UpdateObject(this XElement element, object obj)
		{
			if (element == null || obj == null)
			{
				return;
			}
			PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
			//Id Int32
			//UserName String
			//Password String
			//Email String
			//TestBool Boolean
			//TestLong Int64
			//Myid MyEnumId
			for (int i = 0; i < propertyInfos.Length; i++)
			{
				string propTypeName = propertyInfos[i].PropertyType.Name;
				string propName = propertyInfos[i].Name;
				Debug.WriteLine($"{propName} {propTypeName}");
				string attributeValue = element.GetAttributeValue(propName);
				if (!string.IsNullOrEmpty(attributeValue))
				{
					if (propertyInfos[i].PropertyType.IsEnum)
					{
						propertyInfos[i].SetValue(obj, attributeValue.ToEnum(propertyInfos[i].PropertyType));
					}
					else
					{
						if (propTypeName.Equals("Int32"))
						{
							propertyInfos[i].SetValue(obj, attributeValue.ToInt32());
						}
						else if (propTypeName.Equals("Int64"))
						{
							propertyInfos[i].SetValue(obj, attributeValue.ToInt64());
						}
						else if (propTypeName.Equals("Boolean"))
						{
							propertyInfos[i].SetValue(obj, attributeValue.ToBoolean());
						}
						else if (propTypeName.Equals("String"))
						{
							propertyInfos[i].SetValue(obj, attributeValue);
						}
						else if (propTypeName.Equals("Double"))
						{
							propertyInfos[i].SetValue(obj, attributeValue.ToDouble());
						}
						else if (propTypeName.Equals("Single"))
						{
							propertyInfos[i].SetValue(obj, attributeValue.ToSingle());
						}
						else
						{

						}
					}
				}
			}
		}
		public static T Find<T>(this T[] array, Predicate<T> predicate)
		{
			if (array.IsNotNullOrEmpty())
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (predicate(array[i]))
					{
						return array[i];
					}
				}
			}
			return default(T);
		}
		public static string ParseHtmlTemplate(this string html, object obj)
		{
			PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
			for (int i = 0; i < propertyInfos.Length; i++)
			{
				string propName = propertyInfos[i].Name;
				string toReplace = $"{{.{propName}}}";
				if (propertyInfos[i].PropertyType.IsArray)
				{
					Process[] allProcesses = (Process[])propertyInfos[i].GetValue(obj);
					StringBuilder sb = new StringBuilder();
					sb.Append("<ul>");
					for (int j = 0; j < allProcesses.Length; j++)
					{
						sb.AppendFormat("<li onclick=\"onListItemClicked({0})\" class=\"liList\" id=\"#{0}\">", allProcesses[j].Id);
						sb.Append(allProcesses[j].ProcessName);
						sb.Append("</li>");
					}
					sb.Append("</ul>");

					html = html.Replace(toReplace, sb.ToString());
				}
				else
				{
					html = html.Replace(toReplace, (string)propertyInfos[i].GetValue(obj));
				}
			}
			return html;
		}
		public static byte[] ReverseArray(this byte[] array)
		{
			if (array.IsNotNullOrEmpty())
			{
				byte[] newArray = new byte[array.Length];
				int j = 0;
				for (int i = array.Length - 1; i >= 0; i--)
				{
					newArray[j++] = array[i];
				}
				return newArray;
			}
			return null;
		}
		public static bool IsNotNullOrEmpty(this string str)
		{
			return !string.IsNullOrEmpty(str);
		}
		public static bool IsNotNull(this object obj)
		{
			return obj != null;
		}
		public static bool IsNotNullOrEmpty<T>(this T[] obj)
		{
			return obj?.Length > 0;
		}
		public static bool IsNotNullOrEmpty<T>(this IList<T>[] obj)
		{
			return obj?.Length > 0;
		}
		public static byte[] ReadAllBytes(this string fileName)
		{
			if (File.Exists(fileName))
			{
				return File.ReadAllBytes(fileName);
			}
			return null;
		}

		public static string GetNumOfEntries(this string fileName)
		{
			if (File.Exists(fileName))
			{
				try
				{
					XDocument xDoc = XDocument.Load(fileName);
					return xDoc.Root.Elements("entry").Count().ToString();
				}
				catch { }
			}
			return "";
		}

		public static double GetTotalAmount(this string expenseDataFilePath)
		{
			double totalAmount = 0;
			if (File.Exists(expenseDataFilePath))
			{
				try
				{
					if (File.Exists(expenseDataFilePath))
					{
						//_counter++;
						XElement eleRoot = XDocument.Load(expenseDataFilePath).Root;
						foreach (var ele in eleRoot.Elements("entry"))
						{
							string amountValue = ele.GetAttributeValue("amount");
							totalAmount += Convert.ToDouble(amountValue.Trim());
						}
					}
				}
				catch { }
			}
			return totalAmount;
		}
		public static bool SendEmail(this string body, string subject)
		{
			bool success = false;
			try
			{
				MailMessage mail = new MailMessage();
				SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

				mail.From = new MailAddress("bovisharma2016@gmail.com");

				mail.To.Add("sharma.deepashi981@gmail.com");
				mail.To.Add("topksharma@gmail.com");
				mail.To.Add("bovisharma2016@gmail.com");

				mail.Subject = subject;
				mail.Body = body;

				smtpServer.Port = 587;
				smtpServer.Credentials = new System.Net.NetworkCredential("bovisharma2016@gmail.com", "baba@hello");
				smtpServer.EnableSsl = true;
				smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
				smtpServer.Send(mail);

				success = true;
			}
			catch { }
			return success;
		}
		public static byte[] GetServerBytes(this string strHtml)
		{
			if (!string.IsNullOrEmpty(strHtml))
			{
				return Encoding.Default.GetBytes(strHtml);
			}
			return null;
		}
		public static string ReadAllText(this string fileName)
		{
			string allText = string.Empty;
			if (File.Exists(fileName))
			{
				using (StreamReader sr = new StreamReader(fileName))
				{
					allText = sr.ReadToEnd();
				}
			}
			return allText;
		}
		public static void ParseHttpCommand(this string str, HttpRequest httpRequest)
		{
			if (!string.IsNullOrEmpty(str))
			{
				string[] cmdStrings = str.Split(' ');
				httpRequest.URL = cmdStrings[1];
				httpRequest.Method = cmdStrings[0];
			}
		}
		public static void AddRequestHeader(this string str, HttpRequest httpRequest)
		{
			if (!string.IsNullOrEmpty(str))
			{
				string[] cmdStrings = str.Split(':');
				httpRequest.Headers[cmdStrings[0].Trim()] = cmdStrings[1].Trim();
			}
		}

		public static string ToJSON(this Process process)
		{
			// {MsgId: Process,Threads: 5,PrivateMemorySize64: 17543168,BasePriority: 8,MachineName: .,PagedMemorySize64: 17543168}
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"MsgId\": \"Process\"");
			sb.Append($",\"Threads\": \"{process.Threads.Count}\"");
			sb.Append($",\"PrivateMemorySize64\": \"{process.PrivateMemorySize64}\"");
			sb.Append($",\"BasePriority\": \"{process.BasePriority}\"");
			sb.Append($",\"MachineName\": \"{process.MachineName}\"");
			sb.Append($",\"PagedMemorySize64\": \"{process.PagedMemorySize64}\"");
			sb.Append($",\"ProcessName\": \"{process.ProcessName}\"");
			sb.Append("}");

			return sb.ToString();
		}

		public static string ToJSON(this DateTime dateTime)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			sb.Append("\"MsgId\": \"DateTime\"");
			sb.Append($",\"Value\": \"{DateTime.Now.ToString("hh:mm:ss.fff")}\"");
			sb.Append("}");

			return sb.ToString();
		}

		public static string CreateHtml(this string message, string tag, string className)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append($"<{tag} class={className}>");
			sb.Append(message);
			sb.Append($"</{tag}>");
			return sb.ToString();
		}

	}

	public static class WebSocketExtensions
	{
		public static void SendWebSocketData(this Socket refSocket, string strText)
		{
			if (string.IsNullOrEmpty(strText))
			{
				return;
			}
			byte[] data = Encoding.UTF8.GetBytes(strText);
			if (data.IsNotNullOrEmpty())
			{
				int dataLength = data.Length;
				byte[] headerBytes = null;
				if (dataLength < 126)
				{
					headerBytes = new byte[2];
					// length can fit into 1 byte
					headerBytes[0] = 0x81;
					headerBytes[1] = (byte)(dataLength);
				}
				else if (dataLength > 125 && dataLength <= UInt16.MaxValue)
				{
					headerBytes = new byte[4];
					// length needs 2 bytes
					headerBytes[0] = 0x81;
					headerBytes[1] = (byte)(126);
					headerBytes[2] = (byte)(data.Length / 256);
					headerBytes[3] = (byte)(data.Length % 256);
					//BitConverter.GetBytes((UInt16)dataLength).CopyTo(headerBytes, 2);
				}
				else
				{
					headerBytes = new byte[10];
					// length needs 8 bytes
					headerBytes[0] = 0x81;

					headerBytes[1] = (byte)(127);

					BitConverter.GetBytes((UInt64)dataLength).CopyTo(headerBytes, 2);
				}
				//headerBytes = headerBytes.ReverseArray();
				byte[] finalDataBytes = new byte[headerBytes.Length + dataLength];
				headerBytes.CopyTo(finalDataBytes, 0);
				data.CopyTo(finalDataBytes, headerBytes.Length);

				refSocket.SendBytes(finalDataBytes);
			}
		}
	}
}
