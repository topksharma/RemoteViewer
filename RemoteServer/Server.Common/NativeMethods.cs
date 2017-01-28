using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SrvCommon
{
	public static class NativeMethods
	{
		public const string WIN_DLL = "user32.dll";
		// Declarations
		[DllImport("Gdi32.dll")]
		internal static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
			int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

		[DllImport(WIN_DLL)]
		private static extern IntPtr GetDC(IntPtr hwnd);
		[DllImport(WIN_DLL)]
		private static extern IntPtr ReleaseDC(IntPtr HDC);
		public const int SRCCOPY = 0x00CC0020;

		public static Bitmap CaptureScreen(Rectangle rectangle)
		{
			//use a zero pointer to get hold of the screen context
			IntPtr hdcSrc = GetDC(IntPtr.Zero);
			Bitmap bmpCapture = new Bitmap(rectangle.Width, rectangle.Height);
			//get graphics from bitmap
			using (Graphics grCapture = Graphics.FromImage(bmpCapture))
			{
				IntPtr hdcDest = grCapture.GetHdc();
				// Blit the image data
				BitBlt(hdcDest, 0, 0, rectangle.Width, rectangle.Height, hdcSrc, rectangle.Left, rectangle.Top, SRCCOPY);
				grCapture.ReleaseHdc(hdcDest);
			}
			ReleaseDC(hdcSrc);
			return bmpCapture;
		}
	}
}
