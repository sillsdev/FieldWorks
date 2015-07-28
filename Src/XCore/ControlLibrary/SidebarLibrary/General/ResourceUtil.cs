// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Resources;

namespace SidebarLibrary.General
{
	public class ResourceUtil
	{
		public static Icon LoadIconStream(Type assemblyType, string iconName)
		{
			// Get the assembly that contains the bitmap resource
			Assembly myAssembly = Assembly.GetAssembly(assemblyType);

			// Get the resource stream containing the images
			using (Stream iconStream = myAssembly.GetManifestResourceStream(iconName))
			{
				// Load the Icon from the stream
				return new Icon(iconStream);
			}
		}

		public static Icon LoadIconStream(Type assemblyType, string iconName, Size iconSize)
		{
			// Load the entire Icon requested (may include several different Icon sizes)
			using (Icon rawIcon = LoadIconStream(assemblyType, iconName))
			{
				// Create and return a new Icon that only contains the requested size
				return new Icon(rawIcon, iconSize);
			}
		}

		public static Bitmap LoadBitmapStream(Type assemblyType, string imageName)
		{
			return LoadBitmapStream(assemblyType, imageName, false, new Point(0,0));
		}

		public static Bitmap LoadBitmapStream(Type assemblyType, string imageName, Point transparentPixel)
		{
			return LoadBitmapStream(assemblyType, imageName, true, transparentPixel);
		}

		public static ImageList LoadImageListStream(Type assemblyType,
												string imageName,
												Size imageSize)
		{
			return LoadImageListStream(assemblyType, imageName, imageSize, false, new Point(0,0));
		}

		public static ImageList LoadImageListStream(Type assemblyType,
												string imageName,
												Size imageSize,
												Point transparentPixel)
		{
			return LoadImageListStream(assemblyType, imageName, imageSize, true, transparentPixel);
		}

		protected static Bitmap LoadBitmapStream(Type assemblyType, string imageName,
										   bool makeTransparent, Point transparentPixel)
		{
			// Get the assembly that contains the bitmap resource
			Assembly myAssembly = Assembly.GetAssembly(assemblyType);

			// Get the resource stream containing the images
			using (Stream imageStream = myAssembly.GetManifestResourceStream(imageName))
			{
				// Load the bitmap from stream
				Bitmap image = new Bitmap(imageStream);

				if (makeTransparent)
				{
					Color backColor = image.GetPixel(transparentPixel.X, transparentPixel.Y);

					// Make backColor transparent for Bitmap
					image.MakeTransparent(backColor);
				}

				return image;
			}
		}

		public static ImageList LoadImageListStream(Type assemblyType,
												   string imageName,
												   Size imageSize,
												   bool makeTransparent,
												   Point transparentPixel)
		{
			// Create storage for bitmap strip
			ImageList images = new ImageList();

			// Define the size of images we supply
			images.ImageSize = imageSize;

			// Get the assembly that contains the bitmap resource
			Assembly myAssembly = Assembly.GetAssembly(assemblyType);

			// Get the resource stream containing the images
			using (Stream imageStream = myAssembly.GetManifestResourceStream(imageName))
			{
				// Load the bitmap strip from resource
				Bitmap pics = new Bitmap(imageStream);

				if (makeTransparent)
				{
					Color backColor = pics.GetPixel(transparentPixel.X, transparentPixel.Y);

					// Make backColor transparent for Bitmap
					pics.MakeTransparent(backColor);
				}

				// Load them all !
				images.Images.AddStrip(pics);

				return images;
			}
		}

		// The difference between the "LoadXStream" and "LoadXResource" functions is that
		// the load stream functions will load a embedded resource -- a file that you choose
		// to "embed as resource" while the load resource functions work with resource files
		// that have structure (.resX and .resources) and thus can hold several different
		// resource items.

		public static Icon LoadIconResource(Type assemblyType, string resourceHolder, string imageName)
		{
			// Get the assembly that contains the bitmap resource
			Assembly thisAssembly = Assembly.GetAssembly(assemblyType);
			ResourceManager rm = new ResourceManager(resourceHolder, thisAssembly);
			Icon icon = (Icon)rm.GetObject(imageName);
			return icon;
		}

		public static Bitmap LoadBitmapResource(Type assemblyType, string resourceHolder, string imageName)
		{
			// Get the assembly that contains the bitmap resource
			Assembly thisAssembly = Assembly.GetAssembly(assemblyType);
			ResourceManager rm = new ResourceManager(resourceHolder, thisAssembly);
			Bitmap bitmap = (Bitmap)rm.GetObject(imageName);
			return bitmap;
		}


		public static ImageList LoadImageListResource(Type assemblyType, string resourceHolder,
																string imageName, Size imageSize)

		{
			return LoadImageListResource(assemblyType, resourceHolder, imageName, imageSize, false, new Point(0,0));
		}

		public static ImageList LoadImageListResource(Type assemblyType, string resourceHolder,
			string imageName,
			Size imageSize,
			bool makeTransparent,
			Point transparentPixel)
		{
			// Create storage for bitmap strip
			ImageList images = new ImageList();

			// Define the size of images we supply
			images.ImageSize = imageSize;

			// Get the assembly that contains the bitmap resource
			Assembly thisAssembly = Assembly.GetAssembly(assemblyType);
			ResourceManager rm = new ResourceManager(resourceHolder, thisAssembly);
			Bitmap pics = (Bitmap)rm.GetObject(imageName);

			if (makeTransparent)
			{
				Color backColor = pics.GetPixel(transparentPixel.X, transparentPixel.Y);

				// Make backColor transparent for Bitmap
				pics.MakeTransparent(backColor);
			}

			// Load the image
			images.Images.AddStrip(pics);

			return images;
		}




	}
}