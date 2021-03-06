﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleClassicThemeTaskbar.Forms
{
	public partial class IconTest : Form
	{
		[DllImport("user32.dll")]
		static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
		[StructLayout(LayoutKind.Sequential)]
		struct ICONINFO
		{
			/// <summary>
			/// Specifies whether this structure defines an icon or a cursor.
			/// A value of TRUE specifies an icon; FALSE specifies a cursor
			/// </summary>
			public bool fIcon;
			/// <summary>
			/// The x-coordinate of a cursor's hot spot
			/// </summary>
			public Int32 xHotspot;
			/// <summary>
			/// The y-coordinate of a cursor's hot spot
			/// </summary>
			public Int32 yHotspot;
			/// <summary>
			/// The icon bitmask bitmap
			/// </summary>
			public IntPtr hbmMask;
			/// <summary>
			/// A handle to the icon color bitmap.
			/// </summary>
			public IntPtr hbmColor;
		}
		public static IntPtr GetAppIcon(Window wnd, int index)
		{
			IntPtr hwnd = wnd.Handle;
			IntPtr iconHandle = IntPtr.Zero;
			if (index == 0)
				iconHandle = Taskbar.SendMessage(hwnd, Taskbar.WM_GETICON, Taskbar.ICON_SMALL2, 0);
			if (index == 1)
				iconHandle = Taskbar.SendMessage(hwnd, Taskbar.WM_GETICON, Taskbar.ICON_SMALL, 0);
			if (index == 2)
				iconHandle = Taskbar.SendMessage(hwnd, Taskbar.WM_GETICON, Taskbar.ICON_BIG, 0);
			if (index == 3)
				iconHandle = Taskbar.GetClassLongPtr(hwnd, Taskbar.GCL_HICONSM);
			if (index == 4)
				iconHandle = Taskbar.GetClassLongPtr(hwnd, Taskbar.GCL_HICON);
			return iconHandle;
		}

		public IconTest()
		{
			InitializeComponent();
		}

		public IconTest(Window wnd)
		{
			InitializeComponent();

			PictureBox[] pbs = { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5 };
			for (int i = 0; i < 5; i++)
			{
				PictureBox pictureBox = pbs[i];
				IntPtr iconHandle = GetAppIcon(wnd, i);
				if (iconHandle == IntPtr.Zero)
					continue;
				//Bitmap bmp = Bitmap.FromHicon(c);
				ICONINFO ii;
				GetIconInfo(iconHandle, out ii);
				Bitmap bmpIcon = Bitmap.FromHbitmap(ii.hbmColor);
				Rectangle rectBounds = new Rectangle(0, 0, bmpIcon.Width, bmpIcon.Height);
				BitmapData bmData = new BitmapData();
				bmpIcon.LockBits(rectBounds, ImageLockMode.ReadOnly, bmpIcon.PixelFormat, bmData);
				Bitmap bmpAlpha = new Bitmap(bmData.Width, bmData.Height, bmData.Stride, PixelFormat.Format32bppArgb, bmData.Scan0);
				bmpIcon.UnlockBits(bmData);
				if (bmpAlpha.Width < 128)
					bmpAlpha = UpscaleIcon(bmpAlpha, 128);
				pictureBox.Image = bmpAlpha;
			}
			pbs = new []{ pictureBox10, pictureBox9, pictureBox8, pictureBox7, pictureBox6 };
			for (int i = 0; i < 5; i++)
			{
				PictureBox pictureBox = pbs[i];
				IntPtr iconHandle = GetAppIcon(wnd, i);
				if (iconHandle == IntPtr.Zero)
					continue;
				Bitmap bmp = Bitmap.FromHicon(iconHandle);
				if (bmp.Width < 128)
					bmp = UpscaleIcon(bmp, 128);
				pictureBox.Image = bmp;
			}
		}

		public Bitmap UpscaleIcon(Bitmap image, int newSize, bool destroyOldImage = true)
		{
			Bitmap b = new Bitmap(newSize, newSize);
			int scaleFactor = newSize / image.Width;
			for (int x = 0; x < image.Width; x++)
			{
				for (int y = 0; y < image.Height; y++)
				{
					for (int ex = 0; ex < scaleFactor; ex++)
					{
						for (int ey = 0; ey < scaleFactor; ey++)
						{
							b.SetPixel(x * scaleFactor + ex, y * scaleFactor + ey, image.GetPixel(x, y));
						}
					}
				}
			}
			if (destroyOldImage)
				image.Dispose();
			return b;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
