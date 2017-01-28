using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.IO;
using System.Xml.Linq;

namespace Tcp.Common
{
	public class User
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string Email { get; set; }

		public string Name { get; set; }

		public static User FromXml(XElement userElement)
		{
			User user = new User();
			if (userElement != null)
			{
				userElement.UpdateObject(user);
			}
			return user;
		}

		public XElement ToXML()
		{
			return this.ToXml();
		}

		public void Update(User userToBeUpdated)
		{
			this.UserName = userToBeUpdated.UserName;
			this.Name = userToBeUpdated.Name;
			this.Password = userToBeUpdated.Password;
		}
	}
	public class DbManager
	{
		private const string ConnectionString = @"Data Source = PC192\SQLEXPRESS;Initial Catalog = db_chat_app; Integrated Security = True";
		private SqlConnection _sqlConnection;
		private static DbManager _dbManager;
		private DbManager()
		{

		}
		public void OpenSQL()
		{
			// open connection
			_sqlConnection = new SqlConnection(ConnectionString);
			_sqlConnection.Open();
		}

		public static DbManager GetInstance()
		{
			if (_dbManager == null)
			{
				Interlocked.CompareExchange(ref _dbManager, new DbManager(), null);
			}
			return _dbManager;
		}

		public void LoadUsersFromFile(string fileName)
		{
			if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
			{
				try
				{
					XDocument xDoc = XDocument.Load(fileName);
					IEnumerable<XElement> usersElements = xDoc.Root.Elements("User");
					this._cachedUserTable.Clear();
					foreach (XElement userElement in usersElements)
					{
						this._cachedUserTable.Add(User.FromXml(userElement));
					}
				}
				catch (Exception e)
				{
					Logger.Exception(e);
				}
			}
		}

		private List<User> _cachedUserTable = new List<User>();
		public List<User> ReadTable(string tableName)
		{
			if (_sqlConnection.State == ConnectionState.Open)
			{
				List<User> users = new List<User>();
				string cmd = $"select * from {tableName}";
				SqlCommand sqlCommand = new SqlCommand(cmd, _sqlConnection);
				SqlDataReader reader = sqlCommand.ExecuteReader();
				while (reader.Read())
				{
					users.Add(new User
					{
						Id = reader.GetInt32(0),
						UserName = reader.GetString(1),
						Password = reader.GetString(2)
					});
					Console.WriteLine($"{reader.GetValue(0)}: {reader.GetValue(1)}: {reader.GetValue(2)}");
				}
				reader.Close();
				sqlCommand.Clone();
				_cachedUserTable = new List<User>(users);
				return users;
			}
			return null;
		}

		public void AddNewUser(User user)
		{
			lock (_cachedUserTable)
			{
				this._cachedUserTable.Add(user);
				SaveUsers();
			}
		}

		private void SaveUsers()
		{
			XElement root = new XElement("Users");
			for (int i = 0; i < this._cachedUserTable.Count; i++)
			{
				root.Add(this._cachedUserTable[i].ToXML());
			}
			root.Save(Path.Combine(HttpRequest.DATABASE_FOLDER, "users.xml"));
		}

		public void UpdateUser(User userToBeUpdated)
		{
			User userCurrent = this._cachedUserTable?.Find(user => user.Email.Equals(userToBeUpdated.Email, StringComparison.OrdinalIgnoreCase));
			if (userCurrent != null)
			{
				userCurrent.Update(userToBeUpdated);
				this.SaveUsers();
			}
		}
		public bool DoesUserExists(string userEmail)
		{
			//if (_cachedUserTable == null)
			//{
			//	return false;
			//}

			return this._cachedUserTable?.Find(user => user.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase)) != null;
		}

		public ResponseType CheckUserCredentials(string email, string password)
		{
			if (_cachedUserTable == null)
			{
				return ResponseType.NOK;
			}

			User us = _cachedUserTable?.Find(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && user.Password.Equals(password, StringComparison.InvariantCultureIgnoreCase));
			if (us == null)
			{
				return ResponseType.NOK;
			}
			return ResponseType.OK;
		}

		public User GetUser(string email)
		{
			User u = _cachedUserTable?.Find(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
			return u;
		}
	}
}
