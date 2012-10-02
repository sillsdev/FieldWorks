//-------------------------------------------------------------------------------------------------
// <copyright file="HierarchyImages.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// A static class for providing hierarchy images that show up in the Solution Explorer.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Drawing;
	using System.IO;
	using System.Windows.Forms;

	/// <summary>
	/// Provides images that show up in the Solution Explorer for the hierarchy nodes.
	/// </summary>
	public sealed class HierarchyImages
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private const string BitmapName = "SolutionExplorerIcons.bmp";
		private static readonly Type classType = typeof(HierarchyImages);

		private static ImageList imageList;
		private static bool bitmapLoadFailed;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Prevent direct instantiation of this class.
		/// </summary>
		private HierarchyImages()
		{
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		/// The indexes into the icon image list for each image. Don't change the order!
		/// </summary>
		private enum NodeImageIndex
		{
			/// <summary>The closed folder image index.</summary>
			ClosedFolder,

			/// <summary>The open folder image index.</summary>
			OpenFolder,

			/// <summary>The library closed folder image index.</summary>
			ClosedReferenceFolder,

			/// <summary>The library open folder image index.</summary>
			OpenReferenceFolder,

			/// <summary>The library file image index.</summary>
			ReferenceFile,

			/// <summary>The root image index.</summary>
			Project,

			/// <summary>The image index when the project is unavailable.</summary>
			UnavailableProject,

			/// <summary>A blank image.</summary>
			Blank,
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a blank image for a node with no image.
		/// </summary>
		public static Image Blank
		{
			get { return GetImage(NodeImageIndex.Blank); }
		}

		/// <summary>
		/// Gets an image of a closed folder node.
		/// </summary>
		public static Image ClosedFolder
		{
			get { return GetImage(NodeImageIndex.ClosedFolder); }
		}

		/// <summary>
		/// Gets an image of a closed reference folder node.
		/// </summary>
		public static Image ClosedReferenceFolder
		{
			get { return GetImage(NodeImageIndex.ClosedReferenceFolder); }
		}

		/// <summary>
		/// Gets an image of a reference file node.
		/// </summary>
		public static Image ReferenceFile
		{
			get { return GetImage(NodeImageIndex.ReferenceFile); }
		}

		/// <summary>
		/// Gets an image of an open folder node.
		/// </summary>
		public static Image OpenFolder
		{
			get { return GetImage(NodeImageIndex.OpenFolder); }
		}

		/// <summary>
		/// Gets an image of an open reference folder node.
		/// </summary>
		public static Image OpenReferenceFolder
		{
			get { return GetImage(NodeImageIndex.OpenReferenceFolder); }
		}

		/// <summary>
		/// Gets an image of a project node.
		/// </summary>
		public static Image Project
		{
			get { return GetImage(NodeImageIndex.Project); }
		}

		/// <summary>
		/// Gets an image of a project node that is unavailable (the project wasn't loaded).
		/// </summary>
		public static Image UnavailableProject
		{
			get { return GetImage(NodeImageIndex.UnavailableProject); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets an individual image from the image list.
		/// </summary>
		/// <param name="index">The index of the image to retrieve.</param>
		/// <returns>The requested image.</returns>
		private static Image GetImage(NodeImageIndex index)
		{
			// If we previously tried to initialize the image list and failed, then don't try again.
			// We'll just return null, which is an acceptable value to return.
			if (imageList == null && !bitmapLoadFailed)
			{
				Initialize();
			}

			if (imageList != null)
			{
				return imageList.Images[(int)index];
			}

			return null;
		}

		/// <summary>
		/// Initializes the image list by loading a bitmap strip from the resource file.
		/// </summary>
		private static void Initialize()
		{
			// Create and initialize the image list.
			imageList = new ImageList();
			imageList.ImageSize = new Size(16, 16);
			imageList.TransparentColor = Color.Magenta;

			// Load the bitmap strip. The stream and bitmap must be around for the lifetime of
			// the image list so don't use "using" or Dispose them because the image list has
			// a reference to them and will dispose them correctly.
			Type thisType = typeof(HierarchyImages);
			Stream bitmapStream = thisType.Assembly.GetManifestResourceStream(thisType,BitmapName);

			// Check to make sure that the bitmap actually got loaded.
			if (bitmapStream == null)
			{
				Tracer.Fail("The image list for the hierarchy images cannot be found.");
				bitmapLoadFailed = true;
			}
			else
			{
				Bitmap imageStrip = new Bitmap(bitmapStream);

				// Assign the bitmap strip to the image list.
				imageList.Images.AddStrip(imageStrip);
			}
		}
		#endregion
	}
}
