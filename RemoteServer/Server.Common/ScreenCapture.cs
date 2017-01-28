﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server.Common
{
	public class ScreenCapture
	{
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern IntPtr GetDesktopWindow();

		[StructLayout(LayoutKind.Sequential)]
		private struct Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

		//public static Image CaptureDesktop()
		//{
		//	return CaptureWindow(GetDesktopWindow());
		//}
		public static Image CaptureDesktop()
		{
			Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

			Graphics graphics = Graphics.FromImage(bitmap);

			graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

			bitmap.Save(@"c:\desr\aa.jpeg",ImageFormat.Jpeg);
			return bitmap;
		}

		//public static Image CaptureDesktop()
		//{

		//	Rectangle bounds = Screen.GetBounds(Point.Empty);
		//	Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
		//	using (Graphics g = Graphics.FromImage(bitmap))
		//	{
		//		g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
		//	}
		//	//bitmap.Save("c://My_Img.jpg", ImageFormat.Jpeg);
		//	return bitmap;
		//}

		public static Bitmap CaptureActiveWindow()
		{
			return CaptureWindow(GetForegroundWindow());
		}


		public static Bitmap CaptureWindow(IntPtr handle)
		{
			var rect = new Rect();
			GetWindowRect(handle, ref rect);
			var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
			var result = new Bitmap(bounds.Width, bounds.Height);

			using (var graphics = Graphics.FromImage(result))
			{
				graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
			}

			return result;
		}
	}
}
