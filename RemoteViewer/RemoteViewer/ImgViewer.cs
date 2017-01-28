using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteViewer
{
	public partial class ImgViewer : UserControl
	{
		public Bitmap ImageBitmap { get; set; }
		private Bitmap _bmpScreen;
		private Graphics _gBmp;
		public ImgViewer()
		{
			InitializeComponent();
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (_bmpScreen == null)
			{
				_bmpScreen = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
				_gBmp = Graphics.FromImage(_bmpScreen);
			}
			_gBmp.Clear(BackColor);

			if (ImageBitmap != null)
			{
				_gBmp.DrawImage(ImageBitmap, 0, 0);
			}
			e.Graphics.DrawImage(_bmpScreen, 0, 0);
		}

		protected override void OnResize(EventArgs e)
		{
			if (_bmpScreen != null)
			{
				_bmpScreen.Dispose();
				_gBmp.Dispose();
			}

			_bmpScreen = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
			_gBmp = Graphics.FromImage(_bmpScreen);

			Debug.WriteLine($"{ClientRectangle.Width} {ClientRectangle.Height}");
			base.OnResize(e);
		}
	}
}
