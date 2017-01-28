using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using Tcp.Common;
using System.Drawing;
using System.Xml.Linq;
using System.Linq;

namespace Tcp.Common
{
	public class HttpRequest
	{
		//public const string ROOT_FOLDER = @"c:\\myweb\wwwroot";
		public static string ROOT_FOLDER;
		public const string ROOT_PATH = "/";
		public const string BAD_REQ_HTML = @"html/badreq.html";
		public const string GUID_WEBSOCKET = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
		public enum MimeTypes
		{
			Html,
			Text,
			CSS,
			JavaScript,
			Icon,
			PNG
		}

		public enum HttpStatus
		{
			OK = 200
		}

		public enum HttpVerbID
		{
			GET,
			POST
		}

		public Dictionary<string, string> Headers;

		public string URL { get; set; }

		public HttpVerbID HttpVerb { get; set; }

		private string _httpVerb;

		public string Method
		{
			get
			{
				return this._httpVerb;
			}
			set
			{
				this._httpVerb = value;
				if (this._httpVerb == "GET")
				{
					HttpVerb = HttpVerbID.GET;
				}
				else if (this._httpVerb == "POST")
				{
					HttpVerb = HttpVerbID.POST;
				}
			}
		}

		private string _postData;

		public string PostData
		{
			get
			{
				return this._postData;
			}
			set
			{
				this._postData = value;
				if (!string.IsNullOrEmpty(this._postData))
				{
					_postData = this._postData.Replace(AMPERSEND_CHAR, "@");
					_postData = this._postData.Replace(SPACE_CHAR, " ");
					UpdatePostDataCollection(this._postData);
				}
			}
		}

		public byte[] PostDataBytes { get; set; }

		private const string AMPERSEND_CHAR = "%40";
		private const string SPACE_CHAR = "+";

		private Dictionary<string, string> _postDataCollection;

		public static string ServerIP;

		private void UpdatePostDataCollection(string data)
		{
			try
			{

				this._postDataCollection = new Dictionary<string, string>();
				if (data.StartsWith("---"))
				{
					return;
				}
				string[] values = data.Split('&');
				for (int i = 0; i < values.Length; i++)
				{
					string[] keyValue = values[i].Split('=');
					this._postDataCollection[keyValue[0].Trim()] = keyValue[1].Trim();
				}
			}
			catch { }
		}

		public int GetContentLength()
		{
			string contentLength;
			if (this.Headers.TryGetValue("Content-Length", out contentLength))
			{
				return Convert.ToInt32(contentLength);
			}
			return 0;
		}

		public HttpRequest()
		{
			this.Headers = new Dictionary<string, string>();
		}

		private string _remoteHostIPAddress;

		private Socket _refSocket;

		public static byte[] ImageToBytes(Image img)
		{
			ImageConverter converter = new ImageConverter();
			return (byte[])converter.ConvertTo(img, typeof(byte[]));
		}
		private const string USER_LOGOUT = "userlogout";
		public void ServeHttp(Socket refSocket)
		{
			try
			{
				this._refSocket = refSocket;
				byte[] body = null;
				string[] urlQStrings = URL.Split('?');
				URL = urlQStrings[0];
				if (urlQStrings.Length > 1)
				{
					CreateQueryString(urlQStrings[1]);
				}
				string url = URL;
				if (URL.Equals(ROOT_PATH) || URL.Contains(USER_LOGOUT))
				{
					url = Path.Combine(ROOT_FOLDER, "html/login.html");
					//url = Path.Combine(ROOT_FOLDER, "html/register.html");
					//body = GetResponseBodyWithText(url, new { YearsList = CreateYearListHtml(""), ExpenseTable = CreateExpenseTableHtml(""), UserEmail = "topksharma@gmail.com" });
					body = GetResponseBodyWithText(url, new { LoginError = "" });
				}
				else if (URL.Contains("IMG_FULL_VIEW"))
				{
					url = URL.Remove(0, 1);
					url = url.Split(':')[1];
					url = Path.Combine(USER_DATA_PATH, url);
				}
				else if (URL.Contains(IMG_THUMBNAIL))
				{
					url = URL.Remove(0, 1);
					url = url.Replace(IMG_THUMBNAIL, " ").Trim().Remove(0, 2);
					url = Path.Combine(USER_DATA_PATH, url);
					string thumbnailPath = GetThumbnailPath(url);
					if (File.Exists(thumbnailPath))
					{
						// thumbnail is available
						url = thumbnailPath;
					}
					else
					{
						// create thumbnail 
						bool ok = CreateThumbnail(url, thumbnailPath);
						if (ok)
						{
							url = thumbnailPath;
						}
					}
					// send thubnail here
				}
				else if (URL.Contains("PROFILE_PIC"))
				{
					url = URL.Remove(0, 1);
					string userEmail = url.Split(':')[1];
					url = Path.Combine(USER_DATA_PATH, userEmail);
					string profilePic = Path.Combine(url, PROFILE_PIC_FILE_NAME + ".jpg");
					if (File.Exists(profilePic))
					{
						url = profilePic;
					}
					else if (File.Exists(Path.Combine(url, PROFILE_PIC_FILE_NAME + ".jpeg")))
					{
						url = Path.Combine(url, PROFILE_PIC_FILE_NAME + ".jpeg");
					}
					else if (File.Exists(Path.Combine(url, PROFILE_PIC_FILE_NAME + ".png")))
					{
						url = Path.Combine(url, PROFILE_PIC_FILE_NAME + ".png");
					}
					else
					{
						url = Path.Combine(ROOT_FOLDER, "images/noprofile.png");
					}
					//profilePic = Path.Combine(url, PROFILE_PIC_FILE_NAME + ".jpeg");
					//if (File.Exists(profilePic))
					//{
					//	url = profilePic;
					//}
					//profilePic = Path.Combine(url, PROFILE_PIC_FILE_NAME + ".png");
					//if (File.Exists(profilePic))
					//{
					//	url = profilePic;
					//}
					//profilePic = Path.Combine(ROOT_FOLDER, "images/noprofile.png");
					//if (File.Exists(profilePic))
					//{
					//	url = profilePic;
					//}
					// PROFILE_PIC:topksharma @gmail.com HTTP/ 1.1"
				}
				else
				{
					url = URL.Remove(0, 1);
					if (url == "webSocket")
					{
						string webSocketUrl = $"ws://{ServerIP}:{TcpCommon.HTTP_SERVER_PORT}";
						refSocket.SendBytes(GetHttpResponseBytes(Encoding.ASCII.GetBytes(webSocketUrl), MimeTypes.Text, HttpStatus.OK));
						return;
					}
					if (url.StartsWith("favicon"))
					{
						url = Path.Combine(ROOT_FOLDER, "images/favicon.png");
					}
					else if (!url.StartsWith(ECHO_TEXT) && !url.Contains("home.html"))
					{
						url = url.Trim();
						if (GetMimeType(url) == MimeTypes.Html && !url.StartsWith("html"))
						{
							url = Path.Combine("html", url);
						}
						url = Path.Combine(ROOT_FOLDER, url);
						if (!File.Exists(url))
						{
							url = Path.Combine(ROOT_FOLDER, BAD_REQ_HTML);
						}
					}
				}
				if (HttpVerb == HttpVerbID.POST)
				{
					// check post data
					if (URL.Contains("entry_data"))
					{
						// the data is picture data
						this.CreateUserEntryData();
					}
					else if (URL.Contains("sel_year"))
					{
						url = URL.Remove(0, 1);
						string dataResponse = CreateDataForSelectedYear(url);
						body = Encoding.Default.GetBytes(dataResponse);
					}
					else if (URL.Contains("sel_month"))
					{
						url = URL.Remove(0, 1);
						string dataResponse = CreateDataForSelectedMonth(url);
						body = Encoding.Default.GetBytes(dataResponse);
					}
					else if (URL.Contains("SUMMARY_RESULT"))
					{
						url = URL.Remove(0, 1);
						string summaryResponse = CreateSummaryData(url);
						body = Encoding.Default.GetBytes(summaryResponse);
					}
					else if (URL.Contains("SEARCH_RESULTS"))
					{
						url = URL.Remove(0, 1);
						string searchResponse = CreateSearchData(url);
						body = Encoding.Default.GetBytes(searchResponse);
					}
					else if (URL.Contains("SORT_RESULTS"))
					{
						url = URL.Remove(0, 1);
						string sortResponse = CreateSortData(url);
						body = Encoding.Default.GetBytes(sortResponse);
					}
					else if (URL.Contains("DELETE_ENTRY"))
					{
						url = URL.Remove(0, 1);
						//DeleteEntryAndCreateDataForSelectedMonth(url);
						string dataResponse = DeleteEntryAndCreateDataForSelectedMonth(url);
						body = Encoding.Default.GetBytes(dataResponse);
					}
					else if (PostData.Contains("btnRegister"))
					{
						string strName = GetPostDataValue("txtName");
						string strEmail = GetPostDataValue("txtEmail");
						string strUserName = GetPostDataValue("txtUsername");
						string strPassword = GetPostDataValue("txtPassword");
						string strConfirmPassword = GetPostDataValue("txtConfirm");

						string errorMsg = String.Empty;
						if (strName.IsNotNullOrEmpty() && strEmail.IsNotNullOrEmpty() && strUserName.IsNotNullOrEmpty()
							&& strPassword.IsNotNullOrEmpty() && strConfirmPassword.IsNotNullOrEmpty())
						{
							url = Path.Combine(ROOT_FOLDER, "html/register.html");

							if (DbManager.GetInstance().DoesUserExists(strEmail))
							{
								errorMsg = "A user with same email is already exists.";
							}
							else if (!strPassword.Equals(strConfirmPassword))
							{
								errorMsg = "Password does not match with confirm password.";
							}
							else
							{
								url = Path.Combine(ROOT_FOLDER, "html/register_success.html");
								User newUser = new User()
								{
									Email = strEmail,
									Password = strPassword,
									UserName = strUserName,
									Name = strName
								};

								errorMsg = "Congratulations, you are successfully registered.";
								DbManager.GetInstance().AddNewUser(newUser);
							}
						}
						else
						{
							errorMsg = "Some information is missing.";
						}
						body = GetResponseBodyWithText(url, new { LoginError = errorMsg });
					}
					else if (PostData.Contains("btnLogin"))
					{
						// it's a login data, get email & password
						string email = GetPostDataValue("txtEmail");
						string password = GetPostDataValue("txtPassword");
						if (VerifyLogin(email, password))
						{
							User user = DbManager.GetInstance().GetUser(email);
							//_shouldStartDateTimeTimer = true;
							_remoteHostIPAddress = refSocket.RemoteEndPoint.ToString().Split(':')[0];
							this._currentUserName = user.UserName;
							CreateUserDirectory(user.Email);
							// serve home page
							url = Path.Combine(ROOT_FOLDER, "html/home.html");
							body = GetResponseBodyWithText(url, new { YearsList = CreateYearListHtml("", user.Email, ""), ExpenseTable = CreateExpenseTableHtml(), Header = GetHeaderHtml(user) });
							//body = GetResponseBodyWithText(url, new { UserName = _currentUserName, ListProcess = Process.GetProcesses() });
						}
						else
						{
							url = Path.Combine(ROOT_FOLDER, "html/login.html");
							body = GetResponseBodyWithText(url, new { LoginError = "wrong email or password".CreateHtml("p", "pLoginErr") });
							//this._webSocket?.SendWebSocketData(Utils.CreateJSON("Login-Error", "wrong email or password".CreateHtml("p", "pLoginErr")));
							//return;
						}
					}
					else if (PostData.StartsWith("---"))
					{
						string[] dataValues = PostData.Split('\r', '\n');
						List<string> lstData = new List<string>(dataValues.Length);
						for (int i = 0; i < dataValues.Length; i++)
						{
							if (!string.IsNullOrEmpty(dataValues[i]))
							{
								lstData.Add(dataValues[i]);
							}
						}
						dataValues = lstData.ToArray();
						string separator = dataValues[0];
						string userName = string.Empty;
						StreamWriter sw;
						for (int i = 0; i < dataValues.Length; i++)
						{
							if (string.IsNullOrEmpty(dataValues[i]))
							{
								continue;
							}
							if (dataValues[i].StartsWith(separator) && i != (dataValues.Length - 1))
							{
								string str = dataValues[++i];
								while (!str.StartsWith(separator))
								{
									if (str.StartsWith("Content-Disposition"))
									{
										string[] dStrings = str.Split(';');
										string name = dStrings[1];
										string[] aaStrings = name.Trim().Split('=');
										string value = aaStrings[1].Replace("\"", "");
										if (value == "user")
										{
											userName = dataValues[++i];
										}
										else if (value == "upload")
										{
											var fileName = dStrings[2].Replace("\"", "").Split('=')[1];
											string rcvFileName = Path.Combine(Path.Combine(USER_DATA_PATH, userName), fileName);
											string cType = dataValues[++i].Split(':')[1].Trim();
											if (cType == "text/xml")
											{
												sw = new StreamWriter(rcvFileName);
												ReadUntilSeparator(sw, dataValues, ref i, separator);
											}
										}
									}
									if ((i + 1) < dataValues.Length)
									{
										str = dataValues[i + 1];
									}
									else
									{
										break;
									}
								}
							}
						}
					}
					else if (PostData.StartsWith("SHOWFILES"))
					{
						//_shouldStartDateTimeTimer = true;
						url = Path.Combine(ROOT_FOLDER, "html/showFiles.html");
						string email = GetPostDataValue("SHOWFILES");
						string html = url.ReadAllText();
						html = html.ParseHtmlTemplate(new { UserName = email });
						string[] userFiles = GetUserFiles(email);
						if (userFiles != null)
						{
							string toReplace = "{.ListFiles}";
							StringBuilder sb = new StringBuilder(userFiles.Length);
							for (int i = 0; i < userFiles.Length; i++)
							{
								string value = userFiles[i].Replace(Path.Combine(USER_DATA_PATH, email), "").Trim();
								if (value.StartsWith("\\"))
								{
									value = value.Remove(0, 1);
								}
								sb.AppendFormat($"<input type=\"checkbox\" class=\"chkFile\" value=\"{value}\">");
								sb.Append("</input>");
								sb.Append($"  {value}");
								sb.Append("</br>");
							}
							html = html.Replace(toReplace, sb.ToString());
						}
						body = Encoding.Default.GetBytes(html);
					}
					else if (PostData.StartsWith("UPLOADFILES"))
					{
						url = Path.Combine(ROOT_FOLDER, "html/uploadFiles.html");
						string email = GetPostDataValue("UPLOADFILES");
						body = GetResponseBodyWithText(url, new { UserName = email });
					}
					else
					{
						url = Path.Combine(ROOT_FOLDER, BAD_REQ_HTML);
					}
				}
				else if (HttpVerb == HttpVerbID.GET)
				{
					if (url.Contains("home.html"))
					{
						url = Path.Combine(ROOT_FOLDER, "html/home.html");
						body = GetResponseBodyWithText(url, new { UserName = "abc@gmail.com", ListProcess = Process.GetProcesses() });
					}
					else if (url.Contains("register.html"))
					{
						url = Path.Combine(ROOT_FOLDER, "html/register.html");
						body = GetResponseBodyWithText(url, new { LoginError = "" });
					}
				}
				if (body == null)
				{
					body = GetResponseBody(url);
				}
				byte[] response = GetHttpResponseBytes(body, GetMimeType(url), HttpStatus.OK);
				refSocket.SendBytes(response);
			}
			catch (Exception exception)
			{
				byte[] dataBytes = GetHttpResponseBytes(Encoding.Default.GetBytes(CreateExceptionHtml(exception)), MimeTypes.Html, HttpStatus.OK);
				refSocket.SendBytes(dataBytes);
				// ignored, send exception to web
				Debug.WriteLine(exception.Message);
			}
		}

		private string CreateSummaryData(string url)
		{
			url = url.Replace(SPACE_TEXT, " ");
			string[] cmdStrings = url.Split(':');
			if (cmdStrings[0].Trim() == SUMMARY_RESULT)
			{
				string user_email = cmdStrings[1].Trim();
				string selectedYear = cmdStrings[2].Trim();
				string selectedMonth = cmdStrings[3].Trim();

				string folderPath = Path.Combine(USER_DATA_PATH, user_email);
				folderPath = Path.Combine(folderPath, Path.Combine(selectedYear, selectedMonth));
				string expenseDataFilePath = Path.Combine(folderPath, "data.xml");

				if (File.Exists(expenseDataFilePath))
				{
					StringBuilder sb = new StringBuilder();
					sb.Append("<div style=\"overflow - y: visible; overflow - x: hidden\">");
					XElement eleRoot = XDocument.Load(expenseDataFilePath).Root;
					IEnumerable<XElement> sortedElements = eleRoot.Elements("entry");
					var groupedItems = sortedElements.Select(ele => new { BillDate = ele.GetAttributeValue("billdate"), Amount = ele.GetAttributeValue("amount"), Description = ele.GetAttributeValue("description") })
						.GroupBy(ele => ele.BillDate);
					// loop over groups.
					int idCounter = 0;
					foreach (var group in groupedItems)
					{
						++idCounter;
						string panelHeader = group.Key.ToString();
						double totalGroupAmount = 0.0;
						foreach (var value in group)
						{
							totalGroupAmount += double.Parse(value.Amount);
						}
						if (string.IsNullOrEmpty(panelHeader))
						{
							panelHeader = "No Date";
						}
						else
						{
							DateTime dateTimeBill;
							bool ok = DateTime.TryParse(panelHeader, out dateTimeBill);
							if (ok)
							{
								panelHeader = dateTimeBill.ToString("MMMM dd, yyyy");
							}
						}
						panelHeader = $"{panelHeader} (NOK {totalGroupAmount})";
						StringBuilder sbPanel = new StringBuilder();
						string pId = $"{idCounter}";
						sbPanel.Append("<div class=\"panel-group\">");
						sbPanel.Append("<div class=\"panel panel-default\">");
						sbPanel.Append("<div class=\"panel-heading\">");
						sbPanel.Append("<h4 class=\"panel-title\">");
						sbPanel.Append($"<span data-toggle=\"collapse\" data-target=\"#{pId}\" href=\"#{pId}\">{panelHeader}</span>");
						sbPanel.Append("</h4>");
						sbPanel.Append("</div>"); // panel heading
						sbPanel.Append($"<div id=\"{pId}\" class=\"panel-collapse collapse\">");

						sbPanel.Append("<div class=\"panel-body\">");
						sbPanel.Append("<table class =\"table table-hover table-responsive table-bordered\">");
						sbPanel.Append("<thead>");
						sbPanel.Append("<tr>");
						sbPanel.Append("<th>Description</th>");
						sbPanel.Append("<th>Amount</th>");
						sbPanel.Append("<th><i class=\"fa fa-dollar\"></i></th>");
						sbPanel.Append("</tr>");
						sbPanel.Append("</thead>");

						sbPanel.Append("<tbody>");
						foreach (var value in group)
						{
							sbPanel.Append("<tr>");
							sbPanel.Append("<td>");
							sbPanel.Append(value.Description);
							sbPanel.Append("</td>");
							sbPanel.Append("<td>");
							sbPanel.Append(value.Amount);
							sbPanel.Append("</td>");
							sbPanel.Append("<td>");
							sbPanel.Append("NOK");
							sbPanel.Append("</td>");
							sbPanel.Append("</tr>");
						}
						sbPanel.Append("</tbody>");
						sbPanel.Append("</table>");
						sbPanel.Append("</div>"); // panel body
												  //sbPanel.Append("<div class=\"panel-footer\">Panel Footer</div>");
						sbPanel.Append("</div>");
						sbPanel.Append("</div>");
						sbPanel.Append("</div>");

						sb.Append(sbPanel.ToString());
					}
					sb.Append("</div>");
					return sb.ToString();
				}
				//return CreateExpenseTableHtml(folderPath, "", searchFilter, SortByID.None, SortOrderID.None);				
			}
			return "";
		}

		private string GetCollapsiblePanel(string billdate)
		{
			StringBuilder sbPanel = new StringBuilder();
			sbPanel.Append("<div class=\"panel-group\">");
			sbPanel.Append("<div class=\"panel panel-default\">");
			sbPanel.Append("<div class=\"panel-heading\">");
			sbPanel.Append("<h4 class=\"panel-title\">");
			sbPanel.Append("<span data-toggle=\"collapse\" href=\"#collapse1\">Collapsible panel</span>");
			sbPanel.Append("</h4>");
			sbPanel.Append("</div>");
			sbPanel.Append("<div id=\"collapse1\" class=\"panel-collapse collapse\">");
			sbPanel.Append("<div class=\"panel-body\">Panel Body</div>");

			sbPanel.Append("<table class =\"table table-hover table-responsive table-bordered\">");
			sbPanel.Append("<thead>");
			sbPanel.Append("</thead>");
			sbPanel.Append("<tbody>");

			sbPanel.Append("</tbody>");
			sbPanel.Append("</table>");

			sbPanel.Append("<div class=\"panel-footer\">Panel Footer</div>");
			sbPanel.Append("</div>");
			sbPanel.Append("</div>");
			sbPanel.Append("</div>");
			return sbPanel.ToString();
		}

		private string CreateSortData(string url)
		{
			//var sortBy = "Amount:" + sortOrder;
			//var searchCommand = "SORT_RESULTS:" + sortBy + ":" + document.getElementById("pUserEmail").innerText + ":" +
			//document.getElementById("sel_year").value + ":" +
			//document.getElementById("sel_month").value;

			url = url.Replace(SPACE_TEXT, " ");
			string[] cmdStrings = url.Split(':');
			if (cmdStrings[0].Trim() == SORT_RESULTS)
			{
				SortByID sortById = GetSortById(cmdStrings[1].Trim());
				SortOrderID sortOrder = GetSortOrder(cmdStrings[2].Trim());

				string user_email = cmdStrings[3].Trim();
				string selectedYear = cmdStrings[4].Trim();
				string selectedMonth = cmdStrings[5].Trim();

				string folderPath = Path.Combine(USER_DATA_PATH, user_email);
				folderPath = Path.Combine(folderPath, Path.Combine(selectedYear, selectedMonth));
				return CreateExpenseTableHtml(folderPath, "", "", sortById, sortOrder);
			}
			return "";
		}

		private SortOrderID GetSortOrder(string strSortOrder)
		{
			if (strSortOrder == "1")
			{
				return SortOrderID.Asc;
			}
			else if (strSortOrder == "2")
			{
				return SortOrderID.Desc;
			}
			return SortOrderID.None;
		}

		private SortByID GetSortById(string strSortBy)
		{
			if (strSortBy == "Amount")
			{
				return SortByID.Amount;
			}
			else if (strSortBy == "BillDate")
			{
				return SortByID.BillDate;
			}
			return SortByID.None;
		}

		private const string SUMMARY_RESULT = "SUMMARY_RESULT";
		private const string SEARCH_RESULTS = "SEARCH_RESULTS";
		private const string SORT_RESULTS = "SORT_RESULTS";
		private string CreateSearchData(string url)
		{
			url = url.Replace(SPACE_TEXT, " ");
			string[] cmdStrings = url.Split(':');
			if (cmdStrings[0].Trim() == SEARCH_RESULTS)
			{
				string user_email = cmdStrings[2].Trim();
				string selectedYear = cmdStrings[3].Trim();
				string selectedMonth = cmdStrings[4].Trim();
				string searchFilter = cmdStrings[1].Trim();

				string folderPath = Path.Combine(USER_DATA_PATH, user_email);
				folderPath = Path.Combine(folderPath, Path.Combine(selectedYear, selectedMonth));
				return CreateExpenseTableHtml(folderPath, "", searchFilter, SortByID.None, SortOrderID.None);
			}
			return "";
		}

		private bool CreateThumbnail(string url, string thumbnailPath)
		{
			try
			{
				// Load image.
				Image image = Image.FromFile(url);
				// Compute thumbnail size.
				Size thumbnailSize = new Size(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT);
				// Get thumbnail.
				Image thumbnail = image.GetThumbnailImage(thumbnailSize.Width,
					thumbnailSize.Height, null, IntPtr.Zero);
				// Save thumbnail.
				thumbnail.Save(thumbnailPath);
				return true;
			}
			catch (Exception exception)
			{
			}
			return false;
		}

		private const string THUMBNAIL_TEXT = "_thumbnail";
		private string GetThumbnailPath(string url)
		{
			string fileName = Path.GetFileNameWithoutExtension(url);
			string directoryName = Path.GetDirectoryName(url);

			return Path.Combine(directoryName, fileName + THUMBNAIL_TEXT + Path.GetExtension(url));
		}

		private string DeleteEntryAndCreateDataForSelectedMonth(string url)
		{
			try
			{
				url = url.Replace(SPACE_TEXT, " ");
				string[] cmdStrings = url.Split('$');
				if (cmdStrings[0] == "DELETE_ENTRY")
				{
					string email = cmdStrings[1].Trim();
					string year = cmdStrings[2].Trim();
					string month = cmdStrings[3].Trim();
					string timestamp = cmdStrings[4].Trim();

					string dataFilePath = Path.Combine(USER_DATA_PATH, email);
					dataFilePath = Path.Combine(dataFilePath, year);
					string folderPath = Path.Combine(dataFilePath, month);
					dataFilePath = Path.Combine(folderPath, DATA_FILE_NAME);
					if (File.Exists(dataFilePath))
					{
						XDocument xDoc = XDocument.Load(dataFilePath);
						XElement rootElement = xDoc.Root;
						XElement elementToDelete = null;

						foreach (var ele in rootElement.Elements("entry"))
						{
							if (ele.GetAttributeValue("timestamp").Trim() == timestamp.Trim())
							{
								elementToDelete = ele;
								break;
							}
						}
						if (elementToDelete != null)
						{
							elementToDelete.Remove();
						}

						xDoc.Save(dataFilePath);
						StringBuilder sbMsg = new StringBuilder();
						sbMsg.AppendLine($"{email} deleted an entry for {GetMonthText(month)} {year}.");
						sbMsg.AppendLine($"your total expenses now are {GetTotalAmount(rootElement)} NOK.");

						sbMsg.ToString().SendEmail($"An entry is deleted by {email}");
					}
					string yearListHtml = CreateYearListHtml(year, email, month);
					string pathUptoMonth = Path.Combine(USER_DATA_PATH, email);
					pathUptoMonth = Path.Combine(pathUptoMonth, year);
					pathUptoMonth = Path.Combine(pathUptoMonth, month);

					string expenseHtml = CreateExpenseTableHtml(pathUptoMonth, month, "", SortByID.None, SortOrderID.None);
					return $"{yearListHtml}$$$${expenseHtml}";
					//return CreateExpenseTableHtml(pathUptoMonth, "");
				}
			}
			catch (Exception ex)
			{
				// send error to server
			}
			return "Failed to delete entry.";
		}

		private void CreateUserDirectory(string email)
		{
			if (!Directory.Exists(USER_DATA_PATH))
			{
				Directory.CreateDirectory(USER_DATA_PATH);
			}

			if (!Directory.Exists(Path.Combine(USER_DATA_PATH, email)))
			{
				Directory.CreateDirectory(Path.Combine(USER_DATA_PATH, email));
			}
		}

		private string CreateExceptionHtml(Exception exception)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("<!DOCTYPE HTML>");
			sb.Append("<html>");
			sb.Append("<head>");
			sb.Append("<link rel = \"stylesheet\" href = \"css/bootstrap.min.css\">");
			sb.Append("<title> Exception </title>");
			sb.Append("</head>");
			sb.Append("<body style =\"text-align: center; color: red\">");
			sb.Append($"<h3>{exception.GetType().Name} occured. <h3>");
			sb.Append($"<p> <code> {exception.StackTrace} </code></p>");
			sb.Append("<body>");
			sb.Append("</html>");

			return sb.ToString();
		}

		private string CreateDataForSelectedMonth(string url)
		{
			url = url.Replace(SPACE_TEXT, " ");
			string[] cmdStrings = url.Split(':');
			if (cmdStrings[0].Trim() == "sel_month")
			{
				string selectedYear = cmdStrings[1].Trim();
				string selectedMonth = cmdStrings[2].Trim();
				string user_email = cmdStrings[3].Trim();

				string folderPath = Path.Combine(USER_DATA_PATH, user_email);
				folderPath = Path.Combine(folderPath, Path.Combine(selectedYear, selectedMonth));
				if (Directory.Exists(folderPath))
				{
					string yearListHtml = CreateYearListHtml(selectedYear.Trim(), user_email, selectedMonth);
					string expenseHtml = CreateExpenseTableHtml(folderPath, selectedMonth, "", SortByID.None, SortOrderID.None);
					return $"{yearListHtml}$$$${expenseHtml}";
				}
			}
			return "";
		}

		private string CreateDataForSelectedYear(string url)
		{
			url = url.Replace(SPACE_TEXT, " ");
			string[] cmdStrings = url.Split(':');
			if (cmdStrings[0] == "sel_year")
			{
				string selectedYear = cmdStrings[1];
				string userEmail = cmdStrings[2].Trim();

				string targetPath = Path.Combine(USER_DATA_PATH, userEmail);
				targetPath = Path.Combine(targetPath, selectedYear.Trim());
				if (!Directory.Exists(Path.Combine(USER_DATA_PATH, userEmail)))
				{
					Directory.CreateDirectory(Path.Combine(USER_DATA_PATH, userEmail));
				}
				if (!Directory.Exists(targetPath))
				{
					Directory.CreateDirectory(targetPath);
				}
				VerifyMonthFolders(targetPath);
				string yearListHtml = CreateYearListHtml(selectedYear.Trim(), userEmail, "");
				string expenseTableHtml = "";// CreateExpenseTableHtml("");

				//StringBuilder sb = new StringBuilder();
				//sb.Append("{");
				//sb.Append($"\"YearList\": \"{yearListHtml}\"");
				//sb.Append($",\"ExpenseTable\": \"{expenseTableHtml}\"");
				//sb.Append("}");
				//return sb.ToString();
				return yearListHtml;
			}
			return "";
		}
		private string[] MONTH_LIST = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
		private void VerifyMonthFolders(string targetPath)
		{
			for (int i = 0; i < MONTH_LIST.Length; i++)
			{
				Path.Combine(targetPath, MONTH_LIST[i]).CreateDirectory();
			}
		}
		private enum SortByID
		{
			None = 0,
			Amount = 1,
			BillDate = 2
		}

		private enum SortOrderID
		{
			None = 0,
			Asc = 1,
			Desc = 2
		}

		private string CreateExpenseTableHtml(string pathUptoMonth, string selectedMonth, string searchFilter, SortByID sortById, SortOrderID sortOrder)
		{
			//string panelHeading = "Track your expenses";
			//StringBuilder sb = new StringBuilder();
			//string expenseDataFilePath = Path.Combine(pathUptoMonth, "data.xml");

			//sb.Append("<div class= \"panel panel-default panel-table\">"); // open root div

			//sb.Append("<div class=\"panel-heading\">"); // open panel heading
			//sb.Append("<div class=\"row\">");

			//sb.Append("<div class=\"col col-xs-6\">");
			//sb.Append($"<h3 class=\"panel-title\">{panelHeading}</h3>");
			//sb.Append("</div>");

			//sb.Append("<div class=\"col col-xs-6 text-right\">");
			//sb.Append("<button type=\"button\" class=\"btn btn-info btn-lg\" data-toggle=\"modal\" data-target=\"#myModal\" onclick=\"onCreateBtnClicked()\">Create New</button>");
			////sb.Append("<button type=\"button\" class=\"btn btn-sm btn-primary btn-create\">Create New</button>");
			//sb.Append("</div>");

			//sb.Append("</div>"); // close row
			//sb.Append("</div>"); // close panel heading

			//sb.Append("<div class=\"panel-body\">"); // open panel body

			////sb.Append("<div class= \"table-responsive\">");
			//sb.Append("<table class =\"table table-striped table-fixed table-bordered table-list table-hover\" style=\"float:right; clear: right;\">");
			//// head START
			//sb.Append("<thead>");
			//sb.Append("<tr>");
			//sb.Append($"<th> {"#"} </th>");
			//sb.Append($"<th> {"Description"} </th>");
			////sb.Append($"<th> {"Remark"} </th>");
			//sb.Append($"<th> {"Amount"} </th>");
			//sb.Append($"<th> {"Time-stamp"} </th>");
			//sb.Append($"<th> {"Images"} </th>");
			//sb.Append($"<th> {"Action"} </th>");
			//sb.Append("</tr>");
			//sb.Append("</thead>");
			//// head END

			//// body START
			//sb.Append("<tbody>");
			//bool hasFooter = false;
			////_counter = 0;
			//if (File.Exists(expenseDataFilePath))
			//{
			//	//_counter++;
			//	XElement eleRoot = XDocument.Load(expenseDataFilePath).Root;
			//	foreach (var ele in eleRoot.Elements("entry"))
			//	{
			//		hasFooter = true;
			//		sb.Append("<tr>");
			//		sb.Append("<td>");
			//		sb.Append("#");
			//		sb.Append("</td>");

			//		sb.Append("<td>");
			//		sb.Append(ele.GetAttributeValue("description"));
			//		sb.Append("</td>");

			//		//sb.Append("<td>");
			//		//sb.Append(ele.GetAttributeValue("remark"));
			//		//sb.Append("</td>");

			//		sb.Append("<td>");
			//		sb.Append(ele.GetAttributeValue("amount") + " " + ele.GetAttributeValue("currency"));
			//		sb.Append("</td>");

			//		sb.Append("<td>");
			//		sb.Append(ele.GetAttributeValue("timestamp"));
			//		sb.Append("</td>");
			//		// images
			//		sb.Append("<td>");
			//		string[] imgs = ele.GetAttributeValue("pics").Split(',');
			//		sb.Append(GetImagesHtml(imgs, pathUptoMonth));
			//		sb.Append("</td>");
			//		// Action
			//		sb.Append("<td>");
			//		sb.Append(GetActionHtml(imgs[0], pathUptoMonth));
			//		sb.Append("</td>");
			//		//
			//		sb.Append("</tr>");
			//	}
			//}
			//sb.Append("</tbody>");
			//// body END

			//sb.Append("</table>");
			//sb.Append("</div>"); // close panel body
			//sb.Append("</div>"); // close panel

			//sb.Append("<div class= \"panel panel-default panel-table\">"); // open root div
			//sb.Append("<div class=\"panel-footer\">"); // open panel footer

			//sb.Append("<div class=\"row\">");
			//if (hasFooter)
			//{
			//	sb.Append("<div class=\"col col-xs-4\">");
			//	sb.Append("Page 1 of 5");
			//	sb.Append("</div>"); // close column
			//	sb.Append("<div class=\"col col-xs-8\">");
			//	sb.Append("<ul class=\"pagination hidden-xs pull-right\">");
			//	sb.Append("<li><a href = \"#\" > 1 </ a ></li>");
			//	sb.Append("<li><a href=\"#\">2</a></li>");
			//	sb.Append("<li><a href = \"#\"> 3 </a></li>");
			//	sb.Append("<li><a href=\"#\">4</a></li>");
			//	sb.Append("<li><a href = \"#\" > 5 </ a ></li>");
			//	sb.Append("</ul>");
			//	sb.Append("<ul class=\"pagination visible-xs pull-right\">");
			//	sb.Append("<li><a href = \"#\" >«</a></li>");
			//	sb.Append("<li><a href = \"#\" >»</a></li>");
			//	sb.Append("</ul>");
			//	sb.Append("</div>"); // close paging
			//}
			//else
			//{
			//	sb.Append("<div class=\"col col-xs-12\">");
			//	sb.Append("Page 0 of 0");
			//	sb.Append("</div>"); // close column
			//}
			//sb.Append("</div>"); // close row
			//sb.Append("</div>"); // close panel footer
			//sb.Append("</div>"); // close root div

			//return sb.ToString();

			////sb.Append("<div class = well>");
			////sb.Append("<p class = pNoData>");
			////sb.Append("There is no data available to show.");
			////sb.Append("</p>");
			////sb.Append("</div>");

			////return sb.ToString();

			/*******/

			bool hasFooter = false;
			//_counter = 0;
			double totalAmount = 0;
			double currentItemAmount;

			double filterValue = 0;
			bool applyFilter = false;
			if (!string.IsNullOrEmpty(searchFilter))
			{
				double.TryParse(searchFilter, out filterValue);
				applyFilter = true;
			}

			string month = Path.GetFileNameWithoutExtension(pathUptoMonth);
			DirectoryInfo dirInfo = new DirectoryInfo(pathUptoMonth);
			string year = Path.GetFileNameWithoutExtension(dirInfo.Parent.Name);
			string expenseDataFilePath = Path.Combine(pathUptoMonth, "data.xml");
			string searchPlaceHolder = "Filter items by Amount...";
			string panelHeading = $"Your expenses for <strong> {GetMonthText(month)}, {year} = NOK {expenseDataFilePath.GetTotalAmount()} </strong>";
			if (applyFilter)
			{
				panelHeading = $"Showing results for <strong> {GetMonthText(month)}, {year} </strong> where <strong> Amount >= {filterValue} NOK </strong>";
				//panelHeading = $"Showing results where <strong> Amount >= {filterValue} NOK </strong>";
				searchPlaceHolder = $"Amount = {filterValue}";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<div class= \"panel panel-default panel-table\">"); // open root div

			sb.Append("<div class=\"panel-heading\">"); // open panel heading
			sb.Append("<div class=\"row\">");

			sb.Append("<div class=\"col col-xs-8\">");
			sb.Append($"<h3 class=\"panel-title\">{panelHeading}</h3>");
			sb.Append("</div>");

			//sb.Append("<div class=\"col col-xs-4 text-center\">");
			//sb.Append("<div class=\"input-group\">");
			//sb.Append("<input type =\"text\" class=\"form-control\" placeholder=\"Username\" aria- describedby =\"basic-addon1\">");
			//sb.Append("<span class=\"input-group-addon\" id=\"basic-addon1\">@</span>");
			//sb.Append("</div>");
			//sb.Append("</div>");

			sb.Append("<div class=\"col col-xs-4 text-right\">");
			sb.Append("<button type=\"button\" class=\"btn btn-info btn-lg\" data-toggle=\"modal\" data-target=\"#myModal\" onclick=\"onCreateBtnClicked()\">Add New Bill</button>");
			//sb.Append("<button type=\"button\" class=\"btn btn-sm btn-primary btn-create\">Create New</button>");
			sb.Append("</div>");
			sb.Append("</div>"); // close row
			sb.Append("</hr>");
			/*** SEARCH ROW  ***/

			sb.Append("<div class=\"row\">");
			//sb.Append("<div class=\"col-lg-6\">");
			//sb.Append("<div class=\"input-group\">");
			//sb.Append(" <span class=\"input-group-btn\">");
			//sb.Append("<button class=\"btn btn-default\" type=\"button\">Go!</button>");
			//sb.Append("</span>");
			//sb.Append(" <input type = \"text\" class=\"form-control\" placeholder=\"Search for...\">");
			//sb.Append("</div>");
			//sb.Append("</div>");
			sb.Append("<div class=\"col-lg-6\">");
			sb.Append("<div class=\"input-group\">");
			sb.Append($"<input id=\"searchInput\" onkeypress=\"return onSearchBoxKeyup(event)\"  type =\"text\" class=\"form-control\" placeholder=\"{searchPlaceHolder}\">");
			//sb.Append($"<input id=\"searchInput\" onkeyup=\"onSearchBoxKeyup('event')\" type =\"text\" class=\"form-control\" placeholder=\"{searchPlaceHolder}\">");
			//sb.Append("<span class=\"input-group-btn\">");
			//sb.Append("<button class=\"btn btn-default\" type=\"button\"><span class=\"glyphicon glyphicon-search pull-left\"></span></button>");
			//sb.Append("</span>");
			sb.Append("<span class=\"input-group-btn\">");
			sb.Append("<a id=\"searchAnchor\" onclick=\"onSearchButtonClicked()\" href = \"#\" class=\"btn btn-info\"><span class=\"glyphicon glyphicon-search\"></span> Search</a>");
			//sb.Append("<span class=\"glyphicon glyphicon-search pull-left\"></span>");
			sb.Append("</span>");
			sb.Append("</div>");
			sb.Append("</div>");

			sb.Append("<div class=\"col-lg-6\">");
			sb.Append("<div class=\"input-group\">");
			sb.Append("<span class=\"input-group-btn\">");
			sb.Append("<a id=\"summaryAnchor\" onclick=\"onSummaryButtonClicked()\" href = \"#\" class=\"btn btn-info btn-circle\"><span class=\"glyphicon glyphicon-list\"></span> Summary</a>");
			sb.Append("</span>");
			sb.Append("</div>");
			sb.Append("</div>");

			//sb.Append("<div class=\"col-lg-4\">");
			//sb.Append("<button type=\"button\" class=\"btn btn-primary btn-circle btn-lg\"><i class=\"glyphicon glyphicon-list\"></i></button>");
			//sb.Append("</div>");

			sb.Append("</div>"); // close row

			sb.Append("</div>"); // close panel heading

			sb.Append("<div class=\"panel-body fixed-panel\">"); // open panel body

			//sb.Append("<div class= \"table-responsive\">");
			sb.Append("<table class =\"table table-hover table-responsive table-bordered\">");
			//sb.Append("<table class =\"table table-striped table-fixed table-bordered table-list table-hover\" style=\"float:right; clear: right;\">");
			// head START
			sb.Append("<thead>");
			sb.Append("<tr>");
			sb.Append($"<th> {"#"} </th>");
			sb.Append($"<th> {"Description"} </th>");
			//sb.Append($"<th> {"Remark"} </th>");
			//sb.Append($"<th> {"Amount"} </th>");
			sb.Append(HeaderAmountHtml(sortById, sortOrder));
			//sb.Append($"<th> {"Time-stamp"} </th>");
			//< button type = "button" class="btn btn-default btn-sm">
			//<span class="glyphicon glyphicon-sort"></span> Sort
			//</button>

			//sb.Append($"<th>{"BillDate"} <button type =\"button\" onclick=\"onSortByBillDateClicked('1')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort\"></span>  </button></th>");
			//HeaderBillDateHtml(sortById, sortOrder)
			sb.Append(HeaderBillDateHtml(sortById, sortOrder));
			sb.Append($"<th> {"Images"} </th>");
			sb.Append($"<th> {"Action"} </th>");
			sb.Append("</tr>");
			sb.Append("</thead>");
			// head END

			// body START
			sb.Append("<tbody>");

			if (File.Exists(expenseDataFilePath))
			{
				//_counter++;
				XElement eleRoot = XDocument.Load(expenseDataFilePath).Root;

				IEnumerable<XElement> sortedElements = eleRoot.Elements("entry");
				if (sortById != SortByID.None && sortOrder != SortOrderID.None)
				{
					sortedElements = GetSortedEntries(sortedElements, sortById, sortOrder);
				}
				foreach (var ele in sortedElements)
				{
					string amountValue = ele.GetAttributeValue("amount");
					currentItemAmount = Convert.ToDouble(amountValue.Trim());
					if (applyFilter && currentItemAmount < filterValue)
					{
						continue;
					}
					hasFooter = true;
					if (!applyFilter && currentItemAmount > DANGER_AMOUNT)
					{
						sb.Append("<tr class=\"danger\">");
					}
					else
					{
						sb.Append("<tr>");
					}
					sb.Append("<td>");
					sb.Append("#");
					sb.Append("</td>");

					sb.Append("<td>");
					sb.Append(ele.GetAttributeValue("description"));
					sb.Append("</td>");

					//sb.Append("<td>");
					//sb.Append(ele.GetAttributeValue("remark"));
					//sb.Append("</td>");

					sb.Append("<td>");
					sb.Append(amountValue + " " + ele.GetAttributeValue("currency"));
					sb.Append("</td>");
					if (!string.IsNullOrEmpty(amountValue))
					{
						totalAmount += currentItemAmount;
					}
					//// timestamp
					//sb.Append("<td>");
					//sb.Append(ele.GetAttributeValue("timestamp"));
					//sb.Append("</td>");
					// bill date
					sb.Append("<td>");
					string billDate = ele.GetAttributeValue("billdate");
					if (!string.IsNullOrEmpty(billDate))
					{
						DateTime dateTimeBill;
						bool ok = DateTime.TryParse(billDate, out dateTimeBill);
						if (ok)
						{
							sb.Append(dateTimeBill.ToString("MMMM dd, yyyy"));
						}
						else
						{
							sb.Append(billDate);
						}
					}
					sb.Append("</td>");
					// images
					sb.Append("<td>");
					string[] imgs = ele.GetAttributeValue("pics").Split(',');
					sb.Append(GetImagesHtml(imgs, pathUptoMonth));
					sb.Append("</td>");
					// Action
					sb.Append("<td>");
					sb.Append(GetActionHtml(ele.GetAttributeValue("timestamp")));
					sb.Append("</td>");
					//
					sb.Append("</tr>");
				}
			}
			sb.Append("</tbody>");
			// body END

			sb.Append("</table>");
			sb.Append("</div>"); // close panel body

			sb.Append("<div class=\"panel-footer\">"); // open panel footer

			sb.Append("<div class=\"row\">");
			if (hasFooter)
			{
				//sb.Append("<div class=\"col col-xs-4\">");
				//sb.Append("Page 1 of 5");
				//sb.Append("</div>"); // close column
				//sb.Append("<div class=\"col col-xs-8\">");
				//sb.Append("<ul class=\"pagination hidden-xs pull-right\">");
				//sb.Append("<li><a href = \"#\" > 1 </ a ></li>");
				//sb.Append("<li><a href=\"#\">2</a></li>");
				//sb.Append("<li><a href = \"#\"> 3 </a></li>");
				//sb.Append("<li><a href=\"#\">4</a></li>");
				//sb.Append("<li><a href = \"#\" > 5 </ a ></li>");
				//sb.Append("</ul>");
				//sb.Append("<ul class=\"pagination visible-xs pull-right\">");
				//sb.Append("<li><a href = \"#\" >«</a></li>");
				//sb.Append("<li><a href = \"#\" >»</a></li>");
				//sb.Append("</ul>");
				//sb.Append("</div>"); // close paging

				sb.Append("<div class=\"col col-xs-12\">");
				sb.Append("<div class=\"alert alert-info\">");
				if (applyFilter)
				{
					sb.Append($"<p style=\"text-align: center;\">Total expenses with filter(<strong> Amount = {filterValue}</strong>) <strong> is NOK {totalAmount} </strong></p>");
				}
				else
				{
					sb.Append($"<p style=\"text-align: center;\">Your total expenses for <strong>{GetMonthText(month)}, {year} = NOK {totalAmount} </strong></p>");
				}
				sb.Append("</div>");
				sb.Append("</div>");
			}
			else
			{
				//sb.Append("<div class=\"col col-xs-12\">");
				//sb.Append("Page 0 of 0");
				//sb.Append("</div>"); // close column

				sb.Append("<div class=\"col col-xs-12\">");
				sb.Append("<div class=\"alert alert-info\">");
				sb.Append($"<p style=\"text-align: center;\">Your have <strong>NO</strong> expenses for <strong>{GetMonthText(month)}, {year}</strong></p>");
				sb.Append("</div>");
				sb.Append("</div>");
			}
			sb.Append("</div>"); // close row
			sb.Append("</div>"); // close panel footer

			return sb.ToString();

			//sb.Append("<div class = well>");
			//sb.Append("<p class = pNoData>");
			//sb.Append("There is no data available to show.");
			//sb.Append("</p>");
			//sb.Append("</div>");

			//return sb.ToString();
		}

		private string HeaderBillDateHtml(SortByID sortById, SortOrderID sortOrder)
		{
			string html = string.Empty;
			html = $"<th>{"BillDate"} <button type =\"button\" onclick=\"onSortByBillDateClicked('1')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort\"></span>  </button></th>";
			if (sortById == SortByID.BillDate)
			{
				if (sortOrder == SortOrderID.Asc)
				{
					html = $"<th>{"BillDate"} <button type =\"button\" onclick=\"onSortByBillDateClicked('2')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort-by-attributes\"></span>  </button></th>";
				}
				else if (sortOrder == SortOrderID.Desc)
				{
					html = $"<th>{"BillDate"} <button type =\"button\" onclick=\"onSortByBillDateClicked('1')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort-by-attributes-alt\"></span>  </button></th>";
				}
			}
			return html;
		}

		private string HeaderAmountHtml(SortByID sortById, SortOrderID sortOrder)
		{
			string html = string.Empty;
			html = $"<th>{"Amount"} <button type =\"button\" onclick=\"onSortByAmountClicked('1')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort\"></span>  </button></th>";
			if (sortById == SortByID.Amount)
			{
				if (sortOrder == SortOrderID.Asc)
				{
					html = $"<th>{"Amount"} <button type =\"button\" onclick=\"onSortByAmountClicked('2')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort-by-attributes\"></span>  </button></th>";
				}
				else if (sortOrder == SortOrderID.Desc)
				{
					html = $"<th>{"Amount"} <button type =\"button\" onclick=\"onSortByAmountClicked('1')\" id=\"btnBillDate\" class=\"btn btn-default btn-sm pull-right tableHeader\"><span class=\"glyphicon glyphicon-sort-by-attributes-alt\"></span>  </button></th>";
				}
			}
			return html;
		}
		private IEnumerable<XElement> GetSortedEntries(IEnumerable<XElement> unSortedElements, SortByID sortById, SortOrderID sortOrder)
		{
			if (sortById == SortByID.None || sortOrder == SortOrderID.None)
			{
				return unSortedElements;
			}
			// create entry list
			List<XElement> listElements = unSortedElements.ToList();
			if (sortById == SortByID.Amount)
			{
				if (sortOrder == SortOrderID.Asc)
				{
					listElements = listElements.Select(ele =>
					new { Element = ele, Amount = Convert.ToDouble(ele.GetAttributeValue("amount").Trim()) }).OrderBy(obj => obj.Amount).Select(obj => obj.Element).ToList();
				}
				else if (sortOrder == SortOrderID.Desc)
				{
					listElements = listElements.Select(ele =>
				new { Element = ele, Amount = Convert.ToDouble(ele.GetAttributeValue("amount").Trim()) }).OrderByDescending(obj => obj.Amount).Select(obj => obj.Element).ToList();
				}
			}
			else if (sortById == SortByID.BillDate)
			{
				if (sortOrder == SortOrderID.Asc)
				{
					listElements = listElements.Select(ele =>
					new { Element = ele, BillDate = ParseBillDate(ele) }).OrderBy(obj => obj.BillDate).Select(obj => obj.Element).ToList();
				}
				else if (sortOrder == SortOrderID.Desc)
				{
					listElements = listElements.Select(ele =>
					new { Element = ele, BillDate = ParseBillDate(ele) }).OrderByDescending(obj => obj.BillDate).Select(obj => obj.Element).ToList();
				}
			}
			return listElements;
		}

		private DateTime ParseBillDate(XElement ele)
		{
			DateTime dateTimeBill = new DateTime(2009, 10, 10);
			string billDate = ele.GetAttributeValue("billdate");
			if (!string.IsNullOrEmpty(billDate))
			{
				bool ok = DateTime.TryParse(billDate, out dateTimeBill);
				if (ok)
				{
					return dateTimeBill;
				}
			}
			return dateTimeBill;
		}


		private const double DANGER_AMOUNT = 500.0;
		private string GetHeaderHtml(User user)
		{

			StringBuilder sb = new StringBuilder();
			//sb.Append("<header class=\"header-user-dropdown\">");
			//sb.Append("<div class=\"header-limiter\">");
			//sb.Append("<h1>");
			//sb.Append("<a href =\"#\">pravo.<span>com</span></a>");
			//sb.Append("</h1>");
			//sb.Append("<div class=\"header-user-menu\">");
			////sb.Append("<img src = \"assets /2.jpg\" alt=\"User Image\"/>");
			//sb.Append("<ul>");
			//sb.Append("<li><a href =\"#\"><span class=\"glyphicon glyphicon-cog pull-right\"></span>Settings</a></li>");
			//sb.Append("<li><a href =\"#\"><span class=\"glyphicon glyphicon-log-out pull-right\"></span>Logout</a></li>");
			//sb.Append("</ul>");
			//sb.Append("</div>");
			//sb.Append("</div>");
			//sb.Append("<div hidden>");
			//sb.Append("<p id =\"sel_month\" hidden></p>");
			//sb.Append("<p id =\"sel_year\" hidden></p>");
			//sb.Append("<p id =\"sel_entry\" hidden></p>");
			//sb.Append($"<p id =\"pUserEmail\" hidden>{email}</p>");
			//sb.Append("</div>");
			//sb.Append("</header>");

			sb.Append("<header>");
			sb.Append("<div class=\"col-sm-10\">");
			string userName = $" [{user.Name}]";
			sb.Append($"<h3 style=\"color:white\">Welcome<i><small style=\"color:goldenrod\">{userName}</small></i></h3>");
			//sb.Append("<div class=\"row\">");
			////sb.Append("<p>");
			//sb.Append("<div class=\"col-sm-2\">");
			//sb.Append("<h1>Welcome</h1>");
			////sb.Append("<a href =\"#\">pravo.<span>com</span></a>");
			////sb.Append("<strong>Welcome</strong>");
			////sb.Append("</h1>");
			//sb.Append("</div>");

			//sb.Append("<div class=\"col-sm-10\">");
			//sb.Append($"<h3 align=\"left\" style=\"margin-right: 70%; margin-top:1%;color:darkgoldenrod\">{user.Name}</h3>");
			//sb.Append("</div>");

			////sb.Append("<div class=\"col-sm-4\">");
			////sb.Append("<h4>Expense Manager</h4>");
			////sb.Append("</div>");

			////sb.Append("</p>");
			//sb.Append("</div>"); // row
			sb.Append("</div>"); // column

			sb.Append("<div class=\"col-sm-1\">");
			sb.Append("<div class=\"hidden-xs pull-right\">");
			sb.Append("<a href = \"#\" class=\"dropdown-toggle\" data-toggle=\"dropdown\"><h3><i class=\"glyphicon glyphicon-cog\"></i></h3></a>");
			sb.Append("<ul class=\"dropdown-menu\">");
			//sb.Append("<li><a href =\"#\" ><i class=\"glyphicon glyphicon-chevron-right\"></i> Link</a></li>");
			//sb.Append("<li><a href =\"#\" ><i class=\"glyphicon glyphicon-user\"></i> Link</a></li>");
			sb.Append("<li onclick=\"onProfileLinkClicked()\"><a href =\"#\" style=\"margin: 3px\">Profile<span class=\"glyphicon glyphicon-cog pull-right\"></span></a></li>");
			sb.Append("<li><a href =\"userlogout\" style=\"margin: 3px\">Logout<span class=\"glyphicon glyphicon-log-out pull-right\"></span></a></li>");
			sb.Append("</ul>");
			sb.Append("</div>");
			//sb.Append("<div");
			//sb.Append("<img class=\"img-responsive img-circle pull-right vcenter\" style=\"height=100px;width=100px\" src =\"images\\favicon.png\"/>");
			//sb.Append("</div>");

			sb.Append("</div>");

			sb.Append("<div class=\"col-sm-1\">");
			//sb.Append("<img class=\"img-responsive img-circle pull-right vcenter\" style=\"height=100px;width=100px\" src =\"images\\favicon.png\"/>");
			sb.Append($"<img class=\"img-responsive img-circle pull-right vcenter\" style=\"height:50px; width:50px; margin-top: 25%\" src =\"PROFILE_PIC:{user.Email}\"/>");
			sb.Append("</div>");

			sb.Append("<div hidden>");
			sb.Append("<p id =\"sel_month\" hidden></p>");
			sb.Append("<p id =\"sel_year\" hidden></p>");
			sb.Append("<p id =\"sel_entry\" hidden></p>");
			sb.Append($"<p id =\"_username\" hidden>{user.UserName}</p>");
			sb.Append($"<p id =\"_name\" hidden>{user.Name}</p>");
			sb.Append($"<p id =\"pUserEmail\" hidden>{user.Email}</p>");
			sb.Append("</div>");
			sb.Append("</header>");
			return sb.ToString();
		}
		//private int _counter;
		private string GetActionHtml(string entryTimestamp)
		{
			//string imgPathToServer = "_ipic" + "\\" + Path.Combine(pathUptoMonth, imgName).Replace(USER_DATA_PATH, "").Trim();

			StringBuilder sb = new StringBuilder();
			sb.Append("<div class=\"divider\">");
			//sb.Append("<a class=\"btn btn-default\" title=\"Edit\"><em class=\"fa fa-pencil\"></em></a>");
			sb.Append($"<a class=\"btn btn-default\" onclick=\"onEntryDeleteClicked('{entryTimestamp}')\" title=\"Delete\"><em class=\"fa fa-trash\"></em></a>");
			sb.Append("</div>");
			//sb.Append($"<a class=\"btn btn-default\" data-toggle=\"modal\" data-target=\"#zoomedView\" onclick='return showZoomedImage(\"{imgPathToServer}\")' title=\"View\"><em class=\"fa fa-file-image-o\" ></em></a>");
			return sb.ToString();
		}

		private const int THUMBNAIL_WIDTH = 125;
		private const int THUMBNAIL_HEIGHT = 100;
		private string GetImagesHtml(string[] imgs, string folderPath)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append($"<div class = thumbnail style =\"width={THUMBNAIL_WIDTH}px; height={THUMBNAIL_HEIGHT}px; overflow-y: visible; overflow-x: hidden \">");
			sb.Append("<ul style=\" font-size: 0; display:inline-block; zoom:1; \">");
			for (int i = 0; i < imgs.Length; i++)
			{
				string imageName = imgs[i];
				//string imageName = imgs[i].Replace(":", "-");
				//imageName = imageName.Replace(" ", "-");
				string filePath = Path.Combine(folderPath, imageName);
				string imgPathToServer = IMG_THUMBNAIL + "\\" + filePath.Replace(USER_DATA_PATH, "").Trim();
				if (File.Exists(filePath))
				{
					sb.Append("<li>");
					//string imgId = "img" + imageName;
					sb.Append($"<img onclick=\"viewImageClicked('{Path.GetFileName(imgs[i])}')\" id={Path.GetFileNameWithoutExtension(imgs[0])} src={imgPathToServer} height=\"100\" width=\"100\" />");
					//sb.Append($"<img onclick=\"viewImageClicked('{Path.GetFileNameWithoutExtension(imgs[i])}')\" id={Path.GetFileNameWithoutExtension(imgs[0])} src={imgPathToServer} height=\"100\" width=\"100\" />");
					//sb.Append($"<img id={imgId} onclick=\"myImgClicked({imgPathToServer},{imgId})\" src={imgPathToServer} height=\"75\" width=\"75\" class=\"img-thumbnail\"/>");
					//sb.Append($"<img id={imgId} onclick=\"myImgClicked({imgPathToServer})\" target src={imgPathToServer} height=\"75\" width=\"75\" class=\"img-thumbnail\"/>");
					sb.Append("</li>");
				}
			}
			sb.Append("</ul>");
			sb.Append("</div>");

			return sb.ToString();
		}

		private const string IMG_THUMBNAIL = "IMG_THUMBNAIL";
		private string CreateExpenseTableHtml()
		{
			//string path = @"c:\userdata\topksharma@gmail.com";
			StringBuilder sb = new StringBuilder();
			//string[] years = Directory.GetDirectories(path);
			//if (years.Length > 0)
			//{
			//	string selectedYear = years[0];
			//	string[] months = Directory.GetDirectories(selectedYear);
			//	if (months.Length > 0)
			//	{
			//		string selectedMonth = months[0];
			//		string expenseDataFilePath = Path.Combine(selectedMonth, "data.xml");
			//		if (File.Exists(expenseDataFilePath))
			//		{
			sb.Append("<div class= \"table-responsive\">");
			sb.Append("<table class = table table-striped table-bordered table-list table-hover style=\"float:right; clear: right;\">");
			sb.Append("<thead>");
			sb.Append("<tr>");
			sb.Append($"<th> {"#"} </th>");
			sb.Append($"<th> {"Description"} </th>");
			//sb.Append($"<th> {"Remark"} </th>");
			sb.Append($"<th> {"Amount"} </th>");
			//sb.Append($"<th> {"Time-stamp"} </th>");
			sb.Append($"<th> {"BillDate"} </th>");
			sb.Append($"<th> {"Images"} </th>");
			sb.Append("</tr>");
			sb.Append("</thead>");

			sb.Append("</table>");
			sb.Append("</div>");

			return sb.ToString();
			//	}
			//}

			//sb.Append("<div class = well>");
			//sb.Append("<p class = pNoData>");
			//sb.Append("There is no data available to show.");
			//sb.Append("</p>");
			//sb.Append("</div>");
			//}
			//	return sb.ToString();
		}

		private string GetMonthList(string yearPath, string selectedMonth, string userEmail)
		{
			//string user_email = "topksharma@gmail.com";
			StringBuilder sb = new StringBuilder();
			string[] months = Directory.GetDirectories(yearPath);
			string year = Path.GetFileNameWithoutExtension(yearPath);

			if (months.Length == 0)
			{
				string currentMonth = DateTime.Now.Month.ToString();
				string monthPath = Path.Combine(yearPath, currentMonth);
				Directory.CreateDirectory(monthPath);
				months = new[] { monthPath };
			}
			if (months.Length > 0)
			{
				sb.Append("<div>");
				sb.Append("<ul style=\" font-size: 0 \">");
				for (int i = 0; i < MONTH_LIST.Length; i++)
				{
					string monthNum = MONTH_LIST[i];// Path.GetFileNameWithoutExtension(months[i]);
					string monthText = GetMonthText(monthNum.Trim());
					string dataFilePath = Path.Combine(yearPath, monthNum);
					dataFilePath = Path.Combine(dataFilePath, DATA_FILE_NAME);
					string badges = dataFilePath.GetNumOfEntries();
					string badgesHtml = String.Empty;
					if (!string.IsNullOrEmpty(badges))
					{
						badgesHtml = $"<span class=\"badge pull-right\">{badges}</span>";
					}
					if (monthNum == selectedMonth)
					{
						sb.Append($"<li class =\"liSelectedMonth\" style = \"list-style: none;\" onclick =\" onMonthItemClicked('{year}:{monthNum}:{userEmail}') \"> {monthText} {badgesHtml}</li>");
					}
					else
					{
						sb.Append($"<li class = \"liMonth\" style = \"list-style: none;\" onclick =\" onMonthItemClicked('{year}:{monthNum}:{userEmail}') \"> {monthText} {badgesHtml}</li>");
					}
				}
				//for (int i = 0; i < months.Length; i++)
				//{
				//	string monthNum = Path.GetFileNameWithoutExtension(months[i]);
				//	string monthText = GetMonthText(monthNum.Trim());
				//	sb.Append($"<li class = \"liMonth\" style = \"list-style: none;\" onclick =\" onMonthItemClicked('{year}:{monthNum}:{user_email}') \"> {monthText} </li>");
				//}
				sb.Append("</ul>");
				sb.Append("</div>");
			}
			return sb.ToString();
		}

		private string GetMonthText(string trim)
		{
			switch (trim)
			{
				case "1":
					return "January";
				case "2":
					return "February";
				case "3":
					return "March";
				case "4":
					return "April";
				case "5":
					return "May";
				case "6":
					return "June";
				case "7":
					return "July";
				case "8":
					return "August";
				case "9":
					return "September";
				case "10":
					return "October";
				case "11":
					return "November";
				case "12":
					return "December";
			}
			return "";
		}

		private string CreateYearListHtml(string selectedYear, string userEmail, string selectedMonth)
		{
			//string path = @"c:\userdata\topksharma@gmail.com";
			StringBuilder sb = new StringBuilder();
			string path = Path.Combine(USER_DATA_PATH, userEmail);
			string[] years = Directory.GetDirectories(path);
			string currentYear = DateTime.Now.Year.ToString();
			if (years.Length == 0)
			{
				years = new[] { Path.Combine(path, currentYear) };
			}
			//if (string.IsNullOrEmpty(selectedYear))
			//{
			//	selectedYear = currentYear;
			//}
			if (years.Length > 0)
			{
				sb.Append("<div class = \"well\">");
				sb.Append("<ul>");
				for (int i = 0; i < years.Length; i++)
				{
					string year = Path.GetFileNameWithoutExtension(years[i]);
					if (year == selectedYear)
					{
						sb.Append("<img src=\"images\\folder_open.ico\" height=\"30\" width=\"30\" style=\"float:left; clear: left;\"/>");
						sb.Append($"<li class = \"liSelected\" style = \"list-style: none;\" onclick =\" onItemClicked('{year}') \"> {year} </li>");
						sb.Append(GetMonthList(years[i], selectedMonth, userEmail));
					}
					else
					{
						sb.Append("<img src=\"images\\folder_close.ico\" height=\"30\" width=\"30\" style=\"float:left; clear: left;\"/>");
						sb.Append($"<li class = \"liYear\" style = \"list-style: none;\" onclick =\" onItemClicked('{year}') \"> {year} </li>");
					}
				}
				sb.Append("</ul>");
				sb.Append("</div>");
			}
			else
			{
				sb.Append("<div class = well>");
				sb.Append("<p class = pNoData>");
				sb.Append("There is no data available to show.");
				sb.Append("</p>");
				sb.Append("</div>");
			}
			return sb.ToString();
		}
		public void HandleUserEntryData(Socket socket)
		{
			string boundaryID = socket.ReadLine();
			string boundryEnd = boundaryID + "--";

			if (string.IsNullOrEmpty(boundaryID))
			{
				return;
			}
			string contentDisposition = string.Empty;
			string contentType = string.Empty;
			// user entry variables
			string description = string.Empty;
			string amount = string.Empty;
			string currency = string.Empty;

			string email = string.Empty;
			string year = string.Empty;
			string month = string.Empty;
			string billDate = String.Empty;
			byte[] imgData = null;
			Tuple<Image, MemoryStream> tupleData = null;

			string imgExtension = ".jpg";
			//
			while (true)
			{
				string line = socket.ReadLine();
				if (line.StartsWith(boundryEnd))
				{
					break;
				}
				if (line.StartsWith("Content-Disposition:"))
				{
					contentDisposition = line;
				}
				else if (line.StartsWith("Content-Type:"))
				{
					contentType = line.Split(':')[1].Trim();
				}
				if (contentType == "image/jpeg" || contentType == "image/png")
				{
					if (contentType == "image/png")
					{
						imgExtension = ".png";
					}
					// read empty line
					socket.ReadLine();
					string[] fileNameAndSize = contentDisposition.Split(';')[2].Trim().Split('-');
					int fileSize = int.Parse(fileNameAndSize[1].Remove(fileNameAndSize[1].Length - 1));
					imgData = new byte[fileSize];
					int bytesRead = socket.ReadSocketBytes(imgData);
					if (imgData.Length != bytesRead)
					{
						imgData = null;
					}
					//tupleData = ByteArrayToImage(imgData);
					contentType = string.Empty;
				}
				else
				{
					string[] contentStrings = contentDisposition.Split(';');
					if (contentStrings.Length < 2)
					{
						continue;
					}
					string attrName = NormalizeString(contentStrings[1]).Split('=')[1].Trim();
					if (attrName.Equals("photos"))
					{
						continue;
					}
					string data = socket.ReadLine();
					StringBuilder sbData = new StringBuilder();
					while (data != boundaryID && data != boundryEnd)
					{
						if (!string.IsNullOrEmpty(data))
						{
							sbData.Append(data);
						}
						data = socket.ReadLine();
					}
					// finished reading the value
					// normalize string and populate entry
					if (attrName == "email")
					{
						email = sbData.ToString();
					}
					if (attrName == "year")
					{
						year = sbData.ToString();
					}
					if (attrName == "month")
					{
						month = sbData.ToString();
					}
					if (attrName == "amount")
					{
						amount = sbData.ToString();
					}
					if (attrName == "currency")
					{
						currency = sbData.ToString();
					}
					if (attrName == "description")
					{
						description = sbData.ToString();
					}
					if (attrName == "billdate")
					{
						billDate = sbData.ToString();
					}
					if (data == boundryEnd)
					{
						break;
					}
				}
				contentDisposition = string.Empty;
			}
			// save user data
			string path = Path.Combine(USER_DATA_PATH, email);
			path.CreateDirectory();
			path = Path.Combine(path, year);
			path.CreateDirectory();
			string targetPath = Path.Combine(path, month);
			targetPath.CreateDirectory();
			string dataFilePath = Path.Combine(targetPath, DATA_FILE_NAME);
			XElement root = null;
			if (!File.Exists(dataFilePath))
			{
				root = new XElement("entries");
				//root.Save(dataFilePath);
			}
			else
			{
				root = XDocument.Load(dataFilePath).Root;
			}
			if (root != null)
			{
				XElement entryElement = new XElement("entry");
				entryElement.AddAttribute("description", description);
				entryElement.AddAttribute("currency", currency);
				entryElement.AddAttribute("amount", amount);
				entryElement.AddAttribute("billdate", billDate);
				string timestamp = DateTime.Now.ToString("u").Replace("Z", "").Trim();
				entryElement.AddAttribute("timestamp", timestamp);
				string imageName = timestamp.Replace(":", "-");
				string imgNameWithoutExt = imageName.Replace(" ", "-");
				imageName = imgNameWithoutExt + imgExtension;
				if (imgData != null)
				{
					//try
					//{
					//	MemoryStream ms = new MemoryStream(imgData);
					//	Image returnImage = Image.FromStream(ms);
					//	ms.Close();
					//}
					//catch (Exception exception) { }
					imgData.SaveToFile(Path.Combine(targetPath, imageName));
					//using (FileStream fs = new FileStream(Path.Combine(targetPath, imageName), FileMode.OpenOrCreate))
					//{
					//	fs.Write(imgData, 0, imgData.Length);
					//}
					//tupleData.Item1.Save(Path.Combine(targetPath, imageName));
					//tupleData.Item2.Close();
					string imgThumbnail = $"{imgNameWithoutExt}_thumbnail{imgExtension}";
					MakeThumbnail(imgData, THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT).SaveToFile(Path.Combine(targetPath, imgThumbnail));
					entryElement.AddAttribute("pics", imageName);
				}
				root.Add(entryElement);
				root.Save(dataFilePath);
				// send OK to server
				socket.SendBytes(GetHttpResponseBytes("Data successfully submitted.".TextToBytes(), MimeTypes.Text, HttpStatus.OK));
				// create email and send it
				StringBuilder sbEmail = new StringBuilder();
				sbEmail.AppendLine($"a new entry is inserted for user {email}, {year}/{GetMonthText(month)} on {DateTime.Now.ToString("MMMM dd, yyyy")} {DateTime.Now.ToString("HH:mm:ss")}");
				sbEmail.AppendLine($"the entry is \"Desc: {description}, Amount: {amount} {currency}\"");
				sbEmail.AppendLine($"total amount for {GetMonthText(month)} so for is {currency} {GetTotalAmount(root)}");
				sbEmail.ToString().SendEmail($"A new entry is added on {DateTime.Now.ToString("dd/MM/yy H:mm:ss")}");
			}
			else
			{
				// send error to server
				socket.SendBytes(GetHttpResponseBytes("Failed to submit data.".TextToBytes(), MimeTypes.Text, HttpStatus.OK));
			}
		}
		private byte[] MakeThumbnail(byte[] myImage, int thumbWidth, int thumbHeight)
		{
			using (MemoryStream ms = new MemoryStream())
			using (Image thumbnail = Image.FromStream(new MemoryStream(myImage)).GetThumbnailImage(thumbWidth, thumbHeight, null, new IntPtr()))
			{
				thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				return ms.ToArray();
			}
		}
		public void HandleUserProfileData(Socket socket)
		{
			try
			{
				string boundaryID = socket.ReadLine();
				string boundryEnd = boundaryID + "--";

				if (string.IsNullOrEmpty(boundaryID))
				{
					return;
				}
				string contentDisposition = string.Empty;
				string contentType = string.Empty;
				// user entry variables
				string password = string.Empty;
				string email = string.Empty;
				string userName = string.Empty;
				string name = string.Empty;

				byte[] imgData = null;
				string imgExtension = ".jpg";
				//
				while (true)
				{
					string line = socket.ReadLine();
					if (line.StartsWith(boundryEnd))
					{
						break;
					}
					if (line.StartsWith("Content-Disposition:"))
					{
						contentDisposition = line;
					}
					else if (line.StartsWith("Content-Type:"))
					{
						contentType = line.Split(':')[1].Trim();
					}
					if (contentType == "image/jpeg" || contentType == "image/png")
					{
						if (contentType == "image/png")
						{
							imgExtension = ".png";
						}
						// read empty line
						socket.ReadLine();
						string[] fileNameAndSize = contentDisposition.Split(';')[2].Trim().Split('-');
						int fileSize = int.Parse(fileNameAndSize[1].Remove(fileNameAndSize[1].Length - 1));
						imgData = new byte[fileSize];
						int bytesRead = socket.ReadSocketBytes(imgData);
						if (imgData.Length != bytesRead)
						{
							imgData = null;
						}
						//tupleData = ByteArrayToImage(imgData);
						contentType = string.Empty;
					}
					else
					{
						string[] contentStrings = contentDisposition.Split(';');
						if (contentStrings.Length < 2)
						{
							continue;
						}
						string attrName = NormalizeString(contentStrings[1]).Split('=')[1].Trim();
						if (attrName.Equals("photos"))
						{
							continue;
						}
						string data = socket.ReadLine();
						StringBuilder sbData = new StringBuilder();
						while (data != boundaryID && data != boundryEnd)
						{
							if (!string.IsNullOrEmpty(data))
							{
								sbData.Append(data);
							}
							data = socket.ReadLine();
						}
						// finished reading the value
						// normalize string and populate entry
						if (attrName == "email")
						{
							email = sbData.ToString();
						}
						if (attrName == "userName")
						{
							userName = sbData.ToString();
						}
						if (attrName == "name")
						{
							name = sbData.ToString();
						}
						if (attrName == "userPassword")
						{
							password = sbData.ToString();
						}
						if (data == boundryEnd)
						{
							break;
						}
					}
					contentDisposition = string.Empty;
				}
				if (string.IsNullOrEmpty(email))
				{
					// send error to server
					socket.SendBytes(GetHttpResponseBytes("Missing email address.".TextToBytes(), MimeTypes.Text, HttpStatus.OK));
				}
				else
				{
					// save user data
					string path = Path.Combine(USER_DATA_PATH, email);
					path.CreateDirectory();
					User newUser = new User
					{
						Email = email.Trim(),
						Name = name.Trim(),
						UserName = userName.Trim(),
						Password = password.Trim()
					};

					DbManager.GetInstance().UpdateUser(newUser);
					if (imgData != null)
					{
						imgData.SaveToFile(Path.Combine(path, "profile" + imgExtension));
					}
					string headerHtml = GetHeaderHtml(newUser);
					byte[] response = GetHttpResponseBytes(headerHtml.GetServerBytes(), MimeTypes.Html, HttpStatus.OK);
					socket.SendBytes(response);
					// send OK to server
					//socket.SendBytes(GetHttpResponseBytes($"Profile for {userName} updated successfully.".TextToBytes(), MimeTypes.Text, HttpStatus.OK));
				}
			}
			catch (Exception exception)
			{
				// send error to server
				socket.SendBytes(GetHttpResponseBytes("Error while updating profile.".TextToBytes(), MimeTypes.Text, HttpStatus.OK));
			}
		}

		private void CreateUserEntryData()
		{
			bool keepReading = true;
			int index = 0;
			string webID = ReadLine(ref index);
			string webIdEnd = webID + "--";

			if (string.IsNullOrEmpty(webID))
			{
				return;
			}
			string contentDisposition = string.Empty;
			string contentType = string.Empty;
			// user entry variables
			string description = string.Empty;
			string amount = string.Empty;
			string currency = string.Empty;

			string email = string.Empty;
			string year = string.Empty;
			string month = string.Empty;

			Tuple<Image, MemoryStream> tupleData = null;
			//
			while (index < PostDataBytes.Length)
			{
				string line = ReadLine(ref index);
				if (line.StartsWith(webIdEnd))
				{
					break;
				}
				if (line.StartsWith("Content-Disposition:"))
				{
					contentDisposition = line;
				}
				else if (line.StartsWith("Content-Type:"))
				{
					contentType = line.Split(':')[1].Trim();
				}
				if (IsStartOfData(index))
				{
					index += 2;
					if (contentType == "image/jpeg")
					{
						string[] fileNameAndSize = contentDisposition.Split(';')[2].Trim().Split('-');
						int fileSize = int.Parse(fileNameAndSize[1].Remove(fileNameAndSize[1].Length - 1));
						//string fileName = fileNameAndSize[0].Trim();
						byte[] imgData = new byte[fileSize];
						Array.Copy(PostDataBytes, index, imgData, 0, imgData.Length);
						tupleData = ByteArrayToImage(imgData);
						contentType = string.Empty;
						index += fileSize;
					}
					else
					{
						string[] contentStrings = contentDisposition.Split(';');
						string attrName = NormalizeString(contentStrings[1]).Split('=')[1].Trim();
						int nextIndex = index;
						string data = ReadLine(index, out nextIndex);
						StringBuilder sbData = new StringBuilder();
						while (data != webID && data != webIdEnd)
						{
							sbData.Append(data);
							index = nextIndex;
							data = ReadLine(index, out nextIndex);
						}
						// finished reading the value
						// normalize string and populate entry
						if (attrName == "email")
						{
							email = sbData.ToString();
						}
						if (attrName == "year")
						{
							year = sbData.ToString();
						}
						if (attrName == "month")
						{
							month = sbData.ToString();
						}
						if (attrName == "amount")
						{
							amount = sbData.ToString();
						}
						if (attrName == "currency")
						{
							currency = sbData.ToString();
						}
						if (attrName == "description")
						{
							description = sbData.ToString();
						}
					}
				}
			}
			// save user data
			string path = Path.Combine(USER_DATA_PATH, email);
			path.CreateDirectory();
			path = Path.Combine(path, year);
			path.CreateDirectory();
			string targetPath = Path.Combine(path, month);
			targetPath.CreateDirectory();
			string dataFilePath = Path.Combine(targetPath, DATA_FILE_NAME);
			XElement root = null;
			if (!File.Exists(dataFilePath))
			{
				root = new XElement("entries");
				//root.Save(dataFilePath);
			}
			else
			{
				root = XDocument.Load(dataFilePath).Root;
			}
			if (root != null)
			{
				XElement entryElement = new XElement("entry");
				entryElement.AddAttribute("description", description);
				entryElement.AddAttribute("currency", currency);
				entryElement.AddAttribute("amount", amount);
				string timestamp = DateTime.Now.ToString("u").Replace("Z", "").Trim();
				entryElement.AddAttribute("timestamp", timestamp);
				string imageName = timestamp.Replace(":", "-");
				imageName = imageName.Replace(" ", "-") + ".jpg";
				if (tupleData != null)
				{
					tupleData.Item1.Save(Path.Combine(targetPath, imageName));
					tupleData.Item2.Close();
					entryElement.AddAttribute("pics", imageName);
				}
				root.Add(entryElement);
				root.Save(dataFilePath);
				//// create email and send it
				//StringBuilder sbEmail = new StringBuilder();
				//sbEmail.AppendLine($"a new entry is inserted for {email}, {year}/{month} at {DateTime.Now.ToString("MMMM dd, yyyy")} {DateTime.Now.ToString("HH:mm:ss")}");
				//sbEmail.AppendLine($"the entry is {description}, {amount}, {currency}");
				//sbEmail.AppendLine($"total expenses so for is {currency} {GetTotalAmount(root)}");

				//sbEmail.ToString().SendEmail($"A new entry is added on {DateTime.Now.ToString("dd/MM/yy H:mm:ss")}");
			}
			else
			{
				// send error
			}
		}

		private double GetTotalAmount(XElement root)
		{
			double totalAmount = 0;
			if (root != null)
			{
				foreach (XElement ele in root.Elements("entry"))
				{
					string amountText = ele.GetAttributeValue("amount");
					double amount = 0;
					if (double.TryParse(amountText, out amount))
					{
						totalAmount += amount;
					}
				}
			}
			return totalAmount;
		}

		private const string DATA_FILE_NAME = "data.xml";
		private string NormalizeString(string str)
		{
			return str.Trim().Replace("\"", " ").Trim();
		}
		public Tuple<Image, MemoryStream> ByteArrayToImage(byte[] byteArrayIn)
		{
			MemoryStream ms = new MemoryStream(byteArrayIn);
			Image returnImage = Image.FromStream(ms);
			//ms.Close();
			return new Tuple<Image, MemoryStream>(returnImage, ms);
		}
		private bool IsStartOfData(int index)
		{
			if (PostDataBytes.IsNotNullOrEmpty())
			{
				return (PostDataBytes.Length >= (index + 2)) && PostDataBytes[index] == 13 && PostDataBytes[index + 1] == 10;
			}
			return false;
		}
		private string ReadLine(int index, out int nextIndex)
		{
			nextIndex = index;
			if (PostDataBytes.IsNotNullOrEmpty())
			{
				StringBuilder sb = new StringBuilder();
				while (index < PostDataBytes.Length)
				{
					int i = index;
					if (PostDataBytes[i] == 13 && PostDataBytes[i + 1] == 10)
					{
						index += 2;
						break;
					}
					sb.Append((char)PostDataBytes[i]);
					index++;
				}
				nextIndex = index;
				return sb.ToString();
			}
			return "";
		}
		private string ReadLine(ref int index)
		{
			if (PostDataBytes.IsNotNullOrEmpty())
			{
				StringBuilder sb = new StringBuilder();
				while (index < PostDataBytes.Length)
				//for (int i = index; i < PostDataBytes.Length; i++)
				{
					int i = index;
					if (PostDataBytes[i] == 13 && PostDataBytes[i + 1] == 10)
					{
						index += 2;
						break;
					}
					sb.Append((char)PostDataBytes[i]);
					index++;
				}
				return sb.ToString();
			}
			return "";
		}

		private Dictionary<string, string> _queryStrings = new Dictionary<string, string>();
		private void CreateQueryString(string urlQString)
		{
			// n=John&n=Susan
			string[] collStrings = urlQString.Split('&');
			for (int i = 0; i < collStrings.Length; i++)
			{
				string[] nameValue = collStrings[i].Split('=');
				this._queryStrings[nameValue[0]] = nameValue[1];
			}
		}

		private void ReadUntilSeparator(StreamWriter sw, string[] dataValues, ref int i, string separator)
		{
			try
			{
				string str = dataValues[++i].Trim();
				while (!str.StartsWith(separator))
				{
					if (str.StartsWith("???"))
					{
						str = str.Replace("???", "").Trim();
					}
					sw.WriteLine(str);
					str = dataValues[++i];
				}
				--i;
			}
			finally
			{
				sw.Close();
			}
		}

		private string _currentUserName;
		public const string USER_DATA_PATH = @"c:\userdata";
		private string[] GetUserFiles(string email)
		{
			string path = Path.Combine(USER_DATA_PATH, email);
			if (Directory.Exists(path))
			{
				return Directory.GetFiles(path);
			}
			return null;
		}

		private bool VerifyLogin(string email, string password)
		{
			return DbManager.GetInstance().CheckUserCredentials(email, password) == ResponseType.OK; //email == "abc@gmail.com" && password == "abc@123";
		}

		private string GetPostDataValue(string key)
		{
			string value;
			if (this._postDataCollection != null && this._postDataCollection.TryGetValue(key, out value))
			{
				return value;
			}
			return string.Empty;
		}

		private const string ECHO_TEXT = "_echo:";

		private MimeTypes GetMimeType(string url)
		{
			if (url.StartsWith(ECHO_TEXT))
			{
				return MimeTypes.Text;
			}
			string ex = Path.GetExtension(url);
			if (ex.EndsWith("html"))
			{
				return MimeTypes.Html;
			}
			if (ex.EndsWith("css"))
			{
				return MimeTypes.CSS;
			}
			if (ex.EndsWith("js"))
			{
				return MimeTypes.JavaScript;
			}
			if (ex.EndsWith("ico"))
			{
				return MimeTypes.Icon;
			}
			if (ex.EndsWith("png"))
			{
				return MimeTypes.PNG;
			}
			return MimeTypes.Text;
		}

		private byte[] GetHttpResponseBytes(byte[] body, MimeTypes mimeType, HttpStatus httpStatus)
		{
			StringBuilder sbHttpResponse = new StringBuilder();
			sbHttpResponse.AppendLine($"HTTP/1.1 {GetHttpStatusCodeText(httpStatus)}");
			sbHttpResponse.AppendLine($"Content-Length: {body.Length}");
			sbHttpResponse.AppendLine($"Content-Type: {GetContentType(mimeType)}");
			sbHttpResponse.AppendLine();
			byte[] responseHeader = Encoding.Default.GetBytes(sbHttpResponse.ToString());
			byte[] httpResponse = new byte[responseHeader.Length + body.Length];

			responseHeader.CopyTo(httpResponse, 0);
			body.CopyTo(httpResponse, responseHeader.Length);

			return httpResponse;
		}

		private string GetContentType(MimeTypes mimeType)
		{
			switch (mimeType)
			{
				case MimeTypes.Html:
					return "text/html";
				case MimeTypes.CSS:
					return "text/css";
				case MimeTypes.JavaScript:
					return "text/javascript";
				case MimeTypes.Text:
					return "text/plain";
				case MimeTypes.Icon:
					return "image/x-icon";
				case MimeTypes.PNG:
					return "image/png";
				default:
					return "text/html";
			}
		}

		private string GetHttpStatusCodeText(HttpStatus httpStatus)
		{
			switch (httpStatus)
			{
				case HttpStatus.OK:
					return "200 OK";
				default:
					return "200 OK";
			}
		}

		private const string SPACE_TEXT = "%20";

		private byte[] GetResponseBody(string fileName)
		{
			if (fileName.StartsWith(ECHO_TEXT))
			{
				string fromClient = fileName.Replace(ECHO_TEXT, "");
				fromClient = fromClient.Replace(SPACE_TEXT, " ");
				string result = $"you typed > {fromClient}";
				return Encoding.Default.GetBytes(result);
			}
			return fileName.ReadAllBytes();
		}

		private byte[] GetResponseBodyWithText(string fileName, object objTemplate)
		{
			string html = fileName.ReadAllText();
			html = html.ParseHtmlTemplate(objTemplate);
			return Encoding.Default.GetBytes(html);
		}

		public string GetHeaderValue(string key)
		{
			string value = string.Empty;
			if (this.Headers.TryGetValue(key, out value))
			{
				return value;
			}
			return value;
		}

		public void SendWebSocketHandshake(Socket socket)
		{
			//HTTP / 1.1 101 Switching Protocols
			//Upgrade: websocket
			//Connection: Upgrade
			//Sec - WebSocket - Accept: s3pPLMBiTxaQ9kYGzzhZRbK + xOo =
			string webSocketClientKey = GetHeaderValue("Sec-WebSocket-Key"); // "dGhlIHNhbXBsZSBub25jZQ==";
			string concatenatedKey = webSocketClientKey + GUID_WEBSOCKET;

			SHA1 sha = SHA1.Create();
			byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenatedKey));
			string handshakeKey = Convert.ToBase64String(bytes);

			StringBuilder sbServerHandshake = new StringBuilder();
			sbServerHandshake.AppendLine("HTTP/1.1 101 Switching Protocols");
			sbServerHandshake.AppendLine("Upgrade: websocket");
			sbServerHandshake.AppendLine("Connection: Upgrade");
			sbServerHandshake.AppendLine($"Sec-WebSocket-Accept: {handshakeKey}");
			sbServerHandshake.AppendLine();

			socket.SendBytes(Encoding.UTF8.GetBytes(sbServerHandshake.ToString()));
			this._webSocket = socket;
			WebSocketData webSocketData = new WebSocketData() { Socket = socket, Data = new byte[2] };
			socket.BeginReceive(webSocketData.Data, 0, webSocketData.Data.Length, SocketFlags.None, WebSocketReceiveCallback, webSocketData);
		}

		private Socket _webSocket;

		private void SendWebSocketData(byte[] data, Socket refSocket)
		{
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

		private Timer _timer;

		private bool _timerStarted;

		private void WebSocketReceiveCallback(IAsyncResult ar)
		{
			WebSocketData webSocketData = ar.AsyncState as WebSocketData;
			if (webSocketData != null)
			{
				try
				{
					int numOfBytesRead = webSocketData.Socket.EndReceive(ar);
					if (numOfBytesRead > 1 && !webSocketData.IsHeaderRead)
					{
						// block until we have read the header frame
						webSocketData.IsFinalFragment = (webSocketData.Data[0] & 0x80) > 0;
						webSocketData.OpCode = (byte)(webSocketData.Data[0] & 0xF);

						webSocketData.IsMasked = (webSocketData.Data[1] & 0x80) > 0;
						webSocketData.PayLoadData_Length = (ulong)(webSocketData.Data[1] & 0x7F);
						byte[] payloadLengthBytes = null;
						if (webSocketData.PayLoadData_Length == 126)
						{
							// next 2 bytes are PayLoad length
							payloadLengthBytes = new byte[2];
							numOfBytesRead = webSocketData.Socket.Receive(payloadLengthBytes);
							if (numOfBytesRead == payloadLengthBytes.Length)
							{
								// we need to reverse the byte array
								webSocketData.PayLoadData_Length = BitConverter.ToUInt16(payloadLengthBytes.ReverseArray(), 0);
							}
						}
						if (webSocketData.PayLoadData_Length == 127)
						{
							// next 8 bytes are payload length
							payloadLengthBytes = new byte[8];
							numOfBytesRead = webSocketData.Socket.Receive(payloadLengthBytes);
							if (numOfBytesRead == payloadLengthBytes.Length)
							{
								webSocketData.PayLoadData_Length = BitConverter.ToUInt64(payloadLengthBytes.ReverseArray(), 0);
							}
						}
						// now read masking-key which is 4 bytes long
						numOfBytesRead = webSocketData.Socket.Receive(webSocketData.MaskingKey);
						if (numOfBytesRead < webSocketData.MaskingKey.Length)
						{
							throw new InvalidOperationException("Not able to read masking-key");
						}
						// now before we read complete fragment we need to check size of the fragment
						if (webSocketData.IsLargeFragment)
						{
							webSocketData.Data = new byte[WebSocketData.MAX_PAYLOAD_THRESHOLD];
						}
						else
						{
							webSocketData.Data = new byte[webSocketData.PayLoadData_Length];
						}
						webSocketData.IsHeaderRead = true;
						// go get rest of the frame
						webSocketData.Socket.BeginReceive(webSocketData.Data, 0, webSocketData.Data.Length, SocketFlags.None, WebSocketReceiveCallback, webSocketData);
					}
					else if (numOfBytesRead > 0)
					{
						webSocketData.NumOfBytesRead += (ulong)numOfBytesRead;
						// we have to unmask the data here
						webSocketData.UnMaskData();
						// now we can convert the byte array to string
						string message = webSocketData.Message;
						Debug.WriteLine($"from client: {message}");
						if (webSocketData.ReadComplete)
						{
							// start reading again
							webSocketData.Reset();
							Debug.WriteLine($"Finished message from client: {message}");
							if (!_timerStarted)
							{
								this._timer = new Timer(TimerCallbackFunction, webSocketData.Socket, TimeSpan.FromMilliseconds(2000), TimeSpan.FromSeconds(2));
								this._timerStarted = true;
							}
							ThreadPool.QueueUserWorkItem(HandleClientMessage, message);
							//if (!string.IsNullOrEmpty(message))
							//{
							//	SendWebSocketData(webSocketData.WebSocketEncoding.GetBytes($"Hey client you send - {message}"), webSocketData.Socket);
							//}
						}
						else
						{
							int arraySize = webSocketData.GetDataArraySize();
							if (arraySize != WebSocketData.MAX_PAYLOAD_THRESHOLD)
							{
								webSocketData.Data = new byte[webSocketData.GetDataArraySize()];
							}
						}
						webSocketData.Socket.BeginReceive(webSocketData.Data, 0, webSocketData.Data.Length, SocketFlags.None, WebSocketReceiveCallback, webSocketData);
					}
				}
				catch (Exception exception)
				{
					Debug.WriteLine(exception.Message);
					webSocketData.Socket.CloseSocket();
				}
			}
		}

		//private bool _shouldStartDateTimeTimer = false;
		private int _currentProcessId;

		private void HandleClientMessage(object state)
		{
			string message = state as string;
			string[] msgParts = message.Split(':');
			if (message.StartsWith("PID"))
			{
				this._currentProcessId = Convert.ToInt32(msgParts[1]);
				if (!this._processTimerCreated)
				{
					_timerProcessDataSender = new Timer(SendProcessDataCallback, null, 2000, 2000);
					this._processTimerCreated = true;
				}
			}
			else if (message.StartsWith("SHOWFILES"))
			{
				//"SHOWFILES:" + userid
			}
			else if (message.StartsWith("LOGIN"))
			{
				string[] credentials = msgParts[1].Split(',');
				if (VerifyLogin(credentials[0], credentials[1]))
				{
					//_shouldStartDateTimeTimer = true;
					_remoteHostIPAddress = this._webSocket.RemoteEndPoint.ToString().Split(':')[0];
					this._currentUserName = credentials[0];
					// serve home page
					string url = Path.Combine(ROOT_FOLDER, "html/home.html");
					byte[] body = GetResponseBodyWithText(url, new { UserName = credentials[0], ListProcess = Process.GetProcesses() });
					byte[] response = GetHttpResponseBytes(body, GetMimeType(url), HttpStatus.OK);
					this._refSocket?.SendBytes(response);
				}
				else
				{
					this._webSocket?.SendWebSocketData(Utils.CreateJSON("Login-Error", "wrong email or password".CreateHtml("p", "pLoginErr")));
				}
				//"SHOWFILES:" + userid
			}
			else if (message.StartsWith("FILE_TRANSFER"))
			{
				ThreadPool.QueueUserWorkItem(StartFileTransfer, msgParts[1]);
			}
		}

		private void StartFileTransfer(object state)
		{
			this._currentUserName = "abc@gmail.com";
			string[] fileNames = (state as string).Split(',');
			for (int i = 0; i < fileNames.Length; i++)
			{
				fileNames[i] = Path.Combine(Path.Combine(USER_DATA_PATH, this._currentUserName), fileNames[i]);
			}
			// find TcpFileReceiver socket
			_remoteHostIPAddress = this._webSocket.RemoteEndPoint.ToString().Split(':')[0];
			TcpFileSender tcpFile = GetSender(this._remoteHostIPAddress);
			if (tcpFile != null && tcpFile.GetSocket() != null && tcpFile.GetSocket().IsSocketConnected())
			{
				tcpFile.WebSocket = this._webSocket;
				tcpFile.AddFiles(fileNames);
				tcpFile.StartSending();
			}
			else
			{
				this._dict.Remove(this._remoteHostIPAddress);
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(this._remoteHostIPAddress), TcpCommon.TCP_FILE_RECEIVER_PORT);
				IAsyncResult iAsyncResult = socket.BeginConnect(
					IPAddress.Parse(this._remoteHostIPAddress),
					TcpCommon.TCP_FILE_RECEIVER_PORT,
					null,
					null); // Connect(endPoint);
				int connectTimeout = 5000;
				bool connected = iAsyncResult.AsyncWaitHandle.WaitOne(5000);
				if (!connected)
				{
					// could not connect
					socket.Close();
					Logger.Log($"could not connect to {this._remoteHostIPAddress}:{TcpCommon.TCP_FILE_RECEIVER_PORT} in {connectTimeout} ms");
				}
				else
				{
					try
					{
						socket.EndConnect(iAsyncResult);
						TcpFileSender tcpFileSender = new TcpFileSender(socket, fileNames);
						tcpFileSender.UserData = USER_DATA_PATH;
						tcpFileSender.WebSocket = this._webSocket;
						tcpFileSender.StartSending();
						this._dict[this._remoteHostIPAddress] = tcpFileSender;
					}
					catch (Exception exception)
					{
						this._webSocket?.SendWebSocketData(Utils.CreateJSON("FilePercentage-Error", exception.Message.CreateHtml("p", "pError")));
						Logger.Exception(exception);
					}
				}
			}
		}

		//private string CreateFileTransferJSON(string msgId, string value)
		//{
		//	StringBuilder sb = new StringBuilder();
		//	sb.Append("{");
		//	sb.Append($"\"MsgId\": \"{msgId}\"");
		//	sb.Append($",\"Value\": \"{value}\"");
		//	sb.Append("}");
		//	return sb.ToString();
		//}

		private TcpFileSender GetSender(string remoteIP)
		{
			TcpFileSender tcpFileSender;
			if (this._dict.TryGetValue(remoteIP, out tcpFileSender))
			{
				return tcpFileSender;
			}
			return null;
		}
		private Dictionary<string, TcpFileSender> _dict = new Dictionary<string, TcpFileSender>();
		private List<TcpFileSender> _tcpFileSenders = new List<TcpFileSender>();

		private Timer _timerProcessDataSender;

		private bool _processTimerCreated;

		public static string DATABASE_FOLDER;

		private string PROFILE_PIC_FILE_NAME = "profile";

		private void SendProcessDataCallback(object state)
		{
			this._timerProcessDataSender.Change(Timeout.Infinite, Timeout.Infinite);
			// find process
			Process currentProcess = Process.GetProcesses().Find(p => p.Id == this._currentProcessId);
			if (currentProcess != null && this._webSocket != null)
			{
				SendWebSocketData(WebSocketData.GetBytes(currentProcess.ToJSON()), this._webSocket);
			}
			this._timerProcessDataSender.Change(500, 2000);
		}

		private void TimerCallbackFunction(object state)
		{
			Socket refSocket = state as Socket;
			if (refSocket != null && refSocket.IsSocketConnected())
			{
				SendWebSocketData(WebSocketData.GetBytes(DateTime.Now.ToJSON()), state as Socket);
			}
			else
			{
				this._timer.Change(Timeout.Infinite, Timeout.Infinite);
				refSocket.CloseSocket();
			}
		}

		public enum WebSocket_OpCode : byte
		{
			Continuation_Frame = 0x0,
			Text = 0x1,
			Binary = 0x2,
			Connection_Close = 0x8,
			Ping = 0x9,
			Pong = 0xA
		}

		private class WebSocketData
		{
			public Encoding WebSocketEncoding
			{
				get
				{
					return Encoding.UTF8;
				}
			}

			public Socket Socket { get; set; }

			public byte[] Data { get; set; }

			public bool IsMasked { get; set; }

			public byte OpCode { get; set; }

			public bool IsHeaderRead { get; set; }

			public bool IsFinalFragment { get; set; }

			public ulong PayLoadData_Length { get; set; }

			public byte[] MaskingKey { get; set; }

			public ulong NumOfBytesRead { get; set; }

			public const int MAX_PAYLOAD_THRESHOLD = 4 * 1024;

			public bool IsLargeFragment
			{
				get
				{
					return PayLoadData_Length > MAX_PAYLOAD_THRESHOLD;
				}
			}

			public WebSocketData()
			{
				MaskingKey = new byte[4];
			}

			public void Reset()
			{
				Data = new byte[2];
				IsHeaderRead = IsFinalFragment = IsMasked = false;
				PayLoadData_Length = 0;
				MaskingKey = new byte[4];
				NumOfBytesRead = 0;
				this._sbMessage.Clear();
			}

			private StringBuilder _sbMessage = new StringBuilder();

			/// <summary>
			/// Unmask's the original data using MaskingKey
			/// </summary>
			public void UnMaskData()
			{
				if (this.Data.IsNotNullOrEmpty() && this.MaskingKey.IsNotNullOrEmpty())
				{
					for (int i = 0; i < this.Data.Length; i++)
					{
						Data[i] = (byte)(this.Data[i] ^ this.MaskingKey[i % 4]);
					}
					this._sbMessage.Append(this.GetText());
				}
			}

			public string Message
			{
				get
				{
					return this._sbMessage.ToString();
				}
			}

			public bool ReadComplete
			{
				get
				{
					return NumOfBytesRead == PayLoadData_Length;
				}
			}

			private string GetText()
			{
				if (OpCode == (byte)WebSocket_OpCode.Text)
				{
					return Encoding.UTF8.GetString(Data, 0, Data.Length);
				}
				return string.Empty;
			}

			public int GetDataArraySize()
			{
				if ((NumOfBytesRead + MAX_PAYLOAD_THRESHOLD) > PayLoadData_Length)
				{
					return (int)(PayLoadData_Length - NumOfBytesRead);
				}
				return MAX_PAYLOAD_THRESHOLD;
			}

			public static byte[] GetBytes(string toJson)
			{
				return Encoding.UTF8.GetBytes(toJson);
			}
		}
	}
}
