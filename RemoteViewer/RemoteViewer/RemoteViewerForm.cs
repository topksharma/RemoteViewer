using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using SrvCommon;
using Message = SrvCommon.Message;

namespace RemoteViewer
{
	public partial class RemoteViewerForm : Form
	{
		private readonly ILogger _logger = new ConsoleLogger();
		private SocketResponseHandler _socketResponseHandler;
		public RemoteViewerForm()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			SetButtonTags();
		}

		private void SetButtonTags()
		{
			//btnView.Tag = new BtnArgs { Button = BtnArgs.ButtonID.VIEW, KeyCode = 112 };
			//btnTBL.Tag = new BtnArgs { Button = BtnArgs.ButtonID.TBL, KeyCode = 113 };
			//btnFUNC.Tag = new BtnArgs { Button = BtnArgs.ButtonID.FUNC, KeyCode = 114 };
			//btnINFO.Tag = new BtnArgs { Button = BtnArgs.ButtonID.INFO, KeyCode = 115 };

			//btnCAL.Tag = new BtnArgs { Button = BtnArgs.ButtonID.CAL, KeyCode = (112 | 8) };
			//btnMEM.Tag = new BtnArgs { Button = BtnArgs.ButtonID.MEM, KeyCode = 119 };
			//btnSTARTSTOP.Tag = new BtnArgs { Button = BtnArgs.ButtonID.START_STOP, KeyCode = 45 };
			//btnPAUSE.Tag = new BtnArgs { Button = BtnArgs.ButtonID.PAUSE, KeyCode = 8 };

			//btnCANCEL.Tag = new BtnArgs { Button = BtnArgs.ButtonID.CANCEL, KeyCode = 27 };
			//btnSETUP.Tag = new BtnArgs { Button = BtnArgs.ButtonID.SETUP, KeyCode = 36 };

			//btnUP.Tag = new BtnArgs { Button = BtnArgs.ButtonID.UP, KeyCode = (32 | 2 | 4) };
			//btnLEFT.Tag = new BtnArgs { Button = BtnArgs.ButtonID.LEFT, KeyCode = (1 | 36) };
			//btnOK.Tag = new BtnArgs { Button = BtnArgs.ButtonID.OK, KeyCode = 13 };
			//btnRIGHT.Tag = new BtnArgs { Button = BtnArgs.ButtonID.RIGHT, KeyCode = (2 | 33 | 4) };
			//btnDOWN.Tag = new BtnArgs { Button = BtnArgs.ButtonID.DOWN, KeyCode = 40 };
		}

		private void toolStripButtonConnect_Click(object sender, EventArgs e)
		{
			string ipAddressText = toolStripTextBox1.Text;
			if (!string.IsNullOrEmpty(ipAddressText))
			{

				try
				{
					IPAddress ipAddress = IPAddress.Parse(ipAddressText);
					Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					EndPoint endPoint = new IPEndPoint(ipAddress, Util.SERVER_PORT);
					ConnectToServerToken connectToServerToken = new ConnectToServerToken()
					{
						Event = new AutoResetEvent(false),
						Socket = socket
					};
					toolStripButtonConnect.Enabled = false;
					IAsyncResult result = socket.BeginConnect(endPoint, CallbackConnectToServer, connectToServerToken);
					bool gotSignalInTime = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));
					if (gotSignalInTime)
					{
						connectToServerToken.Event.WaitOne();
						// now handle socket to Response handler
						_socketResponseHandler = new SocketResponseHandler(socket, _logger);
						_socketResponseHandler.ResponseReceived += SocketResponseHandlerSocketResponseReceived;
						AutoResetEvent signal = new AutoResetEvent(false);
						ThreadPool.QueueUserWorkItem(_socketResponseHandler.HandleCommunication, signal);
						//signal.WaitOne();
						//MessageBox.Show("Connection aborted, try again");
						//toolStripButtonConnect.Enabled = true;
					}
					else
					{
						MessageBox.Show(@"Timedout in connecting to server", @"Connection timedout", MessageBoxButtons.OK);
						toolStripButtonConnect.Enabled = true;
					}

				}
				catch (Exception exception)
				{
					_logger.Log("toolStripButtonConnect_Click-" + exception);
					MessageBox.Show(@"Error while connecting to server.", @"Error", MessageBoxButtons.OK);
					toolStripButtonConnect.Enabled = true;
				}

			}
		}

		private void SocketResponseHandlerSocketResponseReceived(object sender, ResponseEventArgs e)
		{
			if (InvokeRequired)
			{
				Invoke(new Action<object, ResponseEventArgs>(SocketResponseHandlerSocketResponseReceived), new object[] { sender, e });
				return;
			}
			//imgViewer1.ImageBitmap = Image.FromFile(e.FileName);
			Image img = Image.FromFile(e.FileName);
			imgViewer1.ImageBitmap = new Bitmap(img, new Size(imgViewer1.Width, imgViewer1.Height));
			imgViewer1.Invalidate();
			//pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
			//pictureBox1.Image = e.Image;
			////pictureBox1.ImageLocation = e.FileName;
			//pictureBox1.Invalidate();
		}

		private void CallbackConnectToServer(IAsyncResult ar)
		{
			ConnectToServerToken token = ar.AsyncState as ConnectToServerToken;
			try
			{
				token?.Socket.EndConnect(ar);
			}
			finally
			{
				token?.Event.Set();
			}
		}

		private void OnButtonClick(object sender, EventArgs e)
		{
			Button button = sender as Button;
			BtnArgs btnArgs = button?.Tag as BtnArgs;
			if (btnArgs != null)
			{
				_socketResponseHandler?.AddMessage(new MsgData { Id = Message.MESSAGE_ID.BUTTON, X = btnArgs.KeyCode });
			}
		}
	}

	public class BtnArgs
	{
		public enum ButtonID
		{
			VIEW,
			TBL,
			FUNC,
			INFO,
			CAL,
			MEM,
			START_STOP,
			PAUSE,
			CANCEL,
			LEFT,
			RIGHT,
			UP,
			DOWN,
			SETUP,
			OK
		}

		public int KeyCode { get; set; }
		public ButtonID Button { get; set; }
	}

}
