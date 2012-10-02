// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeResourceHelper.cs
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Resources for TE
	/// </summary>
	public class TeResourceHelper : ResourceHelper
	{
		/// <summary>The background color for non-editable text</summary>
		public static readonly Color NonEditableColor = Color.FromArgb(240, 255, 240);
		/// <summary>
		/// This is a color indended to be used as a background for views that contain a mixture of read-only
		/// and editable text, with the editable text indicated by being SystemColors.Window. The initial example is
		/// TE's draft back translation in interlinear mode. The actual color used was copied from a similar view in
		/// OurWord.
		/// </summary>
		public static readonly Color ReadOnlyTextBackgroundColor = Color.FromArgb(245, 222, 179);

		public static readonly string BiblicalTermsResourceName = "BiblicalTerms";

		#region Member variables
		private static ResourceManager s_stringResources;
		private static ResourceManager s_helpResources;
		private static ResourceManager s_toolbarResources;
		private static Cursor s_insertVerseCursor;
		private static Cursor s_rightCursor;
		private ImageList m_teSideBarLargeImages;
		private ImageList m_teSideBarSmallImages;
		private ImageList teMenuToolBarImages;
		private ImageList m_teSideBarTabImages;
		private IContainer components;
		#endregion

		/// <summary>
		/// Side bar index list. Index values are used as an image index for a sidebar button.
		/// </summary>
		/// <remarks>Indices are image index in m_teSideBarLargeImages/m_teSideBarSmallImages</remarks>
		public enum SideBarIndices
		{
			/// <summary>Scripture Draft icon</summary>
			Draft = 0,
			/// <summary>Scripture Print Layout icon</summary>
			PrintLayout,
			/// <summary>Scripture Trial Publication icon</summary>
			TrialPublication,
			/// <summary>Back Translation Draft icon</summary>
			BTDraft,
			/// <summary>Back Translation Print Layout icon</summary>
			BTParallelPrintLayout,
			/// <summary>Checking Key Terms icon</summary>
			KeyTerms,
			/// <summary>Publication icon</summary>
			Publication,
			/// <summary>Data Entry icon</summary>
			DataEntry,
			/// <summary>No filter icon</summary>
			NoFilter,
			/// <summary>Basic filter icon</summary>
			BasicFilter,
			/// <summary>Advanced filter icon</summary>
			AdvancedFilter,
			/// <summary>Sort method icon</summary>
			SortMethod,
			/// <summary>Scripture Correction Print Layout icon</summary>
			CorrectionPrintLayout,
			/// <summary>Back Translation Consultant Check icon</summary>
			BTConsultantCheck,
			/// <summary>Back Translation Simple Print Layout icon</summary>
			BTSimplePrintLayout,
			/// <summary>Checking/Editorial Checks icon</summary>
			EditorialChecks,
		}

		#region Construction and destruction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeResourceHelper"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeResourceHelper()
		{
			InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Dispose static member variables
		/// </summary>
		protected override void DisposeStaticMembers()
		{
			if (s_stringResources != null)
				s_stringResources.ReleaseAllResources();
			s_stringResources = null;
			if (s_helpResources != null)
				s_helpResources.ReleaseAllResources();
			s_helpResources = null;
			if (s_toolbarResources != null)
				s_toolbarResources.ReleaseAllResources();
			s_toolbarResources = null;
			if (s_insertVerseCursor != null)
				s_insertVerseCursor.Dispose();
			s_insertVerseCursor = null;
			if (s_rightCursor != null)
				s_rightCursor.Dispose();
			s_rightCursor = null;
			base.DisposeStaticMembers();
		}

		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TeResourceHelper));
			this.m_teSideBarLargeImages = new System.Windows.Forms.ImageList(this.components);
			this.m_teSideBarSmallImages = new System.Windows.Forms.ImageList(this.components);
			this.teMenuToolBarImages = new System.Windows.Forms.ImageList(this.components);
			this.m_teSideBarTabImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// m_teSideBarLargeImages
			//
			this.m_teSideBarLargeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_teSideBarLargeImages.ImageStream")));
			this.m_teSideBarLargeImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_teSideBarLargeImages.Images.SetKeyName(0, "Scripture Draft");
			this.m_teSideBarLargeImages.Images.SetKeyName(1, "Print Layout");
			this.m_teSideBarLargeImages.Images.SetKeyName(2, "Trial Publication");
			this.m_teSideBarLargeImages.Images.SetKeyName(3, "BT Draft");
			this.m_teSideBarLargeImages.Images.SetKeyName(4, "BT Print Layout");
			this.m_teSideBarLargeImages.Images.SetKeyName(5, "Key Terms");
			this.m_teSideBarLargeImages.Images.SetKeyName(6, "Publication");
			this.m_teSideBarLargeImages.Images.SetKeyName(7, "Data Entry");
			this.m_teSideBarLargeImages.Images.SetKeyName(8, "No Filter");
			this.m_teSideBarLargeImages.Images.SetKeyName(9, "Basic Filter");
			this.m_teSideBarLargeImages.Images.SetKeyName(10, "Advanced Filter");
			this.m_teSideBarLargeImages.Images.SetKeyName(11, "Sort Method");
			this.m_teSideBarLargeImages.Images.SetKeyName(12, "Correction Printout");
			this.m_teSideBarLargeImages.Images.SetKeyName(13, "BT Consultant Check");
			this.m_teSideBarLargeImages.Images.SetKeyName(14, "BT Simple Print Layout");
			this.m_teSideBarLargeImages.Images.SetKeyName(15, "Editorial Checks-Large.png");
			//
			// m_teSideBarSmallImages
			//
			this.m_teSideBarSmallImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_teSideBarSmallImages.ImageStream")));
			this.m_teSideBarSmallImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_teSideBarSmallImages.Images.SetKeyName(0, "Scripture Draft");
			this.m_teSideBarSmallImages.Images.SetKeyName(1, "Print Layout");
			this.m_teSideBarSmallImages.Images.SetKeyName(2, "Trial Publication");
			this.m_teSideBarSmallImages.Images.SetKeyName(3, "BT Draft");
			this.m_teSideBarSmallImages.Images.SetKeyName(4, "BT Print Layout");
			this.m_teSideBarSmallImages.Images.SetKeyName(5, "Key Terms");
			this.m_teSideBarSmallImages.Images.SetKeyName(6, "Publication");
			this.m_teSideBarSmallImages.Images.SetKeyName(7, "Data Entry");
			this.m_teSideBarSmallImages.Images.SetKeyName(8, "No Filter");
			this.m_teSideBarSmallImages.Images.SetKeyName(9, "Basic Filter");
			this.m_teSideBarSmallImages.Images.SetKeyName(10, "Advanced Filter");
			this.m_teSideBarSmallImages.Images.SetKeyName(11, "Sort Method");
			this.m_teSideBarSmallImages.Images.SetKeyName(12, "Correction Printout");
			this.m_teSideBarSmallImages.Images.SetKeyName(13, "BT Consultant Check");
			this.m_teSideBarSmallImages.Images.SetKeyName(14, "BT Simple Print Layout");
			this.m_teSideBarSmallImages.Images.SetKeyName(15, "Editorial Checks-Small.png");
			//
			// teMenuToolBarImages
			//
			this.teMenuToolBarImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("teMenuToolBarImages.ImageStream")));
			this.teMenuToolBarImages.TransparentColor = System.Drawing.Color.Magenta;
			this.teMenuToolBarImages.Images.SetKeyName(0, "");
			this.teMenuToolBarImages.Images.SetKeyName(1, "");
			this.teMenuToolBarImages.Images.SetKeyName(2, "");
			this.teMenuToolBarImages.Images.SetKeyName(3, "");
			this.teMenuToolBarImages.Images.SetKeyName(4, "");
			this.teMenuToolBarImages.Images.SetKeyName(5, "");
			this.teMenuToolBarImages.Images.SetKeyName(6, "");
			this.teMenuToolBarImages.Images.SetKeyName(7, "");
			this.teMenuToolBarImages.Images.SetKeyName(8, "");
			this.teMenuToolBarImages.Images.SetKeyName(9, "");
			this.teMenuToolBarImages.Images.SetKeyName(10, "");
			this.teMenuToolBarImages.Images.SetKeyName(11, "");
			this.teMenuToolBarImages.Images.SetKeyName(12, "");
			this.teMenuToolBarImages.Images.SetKeyName(13, "");
			this.teMenuToolBarImages.Images.SetKeyName(14, "");
			this.teMenuToolBarImages.Images.SetKeyName(15, "");
			this.teMenuToolBarImages.Images.SetKeyName(16, "");
			this.teMenuToolBarImages.Images.SetKeyName(17, "");
			this.teMenuToolBarImages.Images.SetKeyName(18, "");
			this.teMenuToolBarImages.Images.SetKeyName(19, "");
			this.teMenuToolBarImages.Images.SetKeyName(20, "");
			this.teMenuToolBarImages.Images.SetKeyName(21, "");
			this.teMenuToolBarImages.Images.SetKeyName(22, "");
			this.teMenuToolBarImages.Images.SetKeyName(23, "");
			this.teMenuToolBarImages.Images.SetKeyName(24, "");
			this.teMenuToolBarImages.Images.SetKeyName(25, "");
			this.teMenuToolBarImages.Images.SetKeyName(26, "");
			this.teMenuToolBarImages.Images.SetKeyName(27, "");
			this.teMenuToolBarImages.Images.SetKeyName(28, "");
			this.teMenuToolBarImages.Images.SetKeyName(29, "");
			this.teMenuToolBarImages.Images.SetKeyName(30, "");
			this.teMenuToolBarImages.Images.SetKeyName(31, "");
			this.teMenuToolBarImages.Images.SetKeyName(32, "");
			this.teMenuToolBarImages.Images.SetKeyName(33, "");
			this.teMenuToolBarImages.Images.SetKeyName(34, "");
			this.teMenuToolBarImages.Images.SetKeyName(35, "");
			this.teMenuToolBarImages.Images.SetKeyName(36, "Keyterm-Filtered.bmp");
			this.teMenuToolBarImages.Images.SetKeyName(37, "Insert_Response.png");
			this.teMenuToolBarImages.Images.SetKeyName(38, "");
			this.teMenuToolBarImages.Images.SetKeyName(39, "");
			this.teMenuToolBarImages.Images.SetKeyName(40, "");
			this.teMenuToolBarImages.Images.SetKeyName(41, "IgnoredInconsistency");
			this.teMenuToolBarImages.Images.SetKeyName(42, "UnignoredInconsistency");
			this.teMenuToolBarImages.Images.SetKeyName(43, "");
			this.teMenuToolBarImages.Images.SetKeyName(44, "");
			this.teMenuToolBarImages.Images.SetKeyName(45, "UpdateKeyTermEquivalentsImage");
			this.teMenuToolBarImages.Images.SetKeyName(46, "KeyTermMissingRenderedImage");
			this.teMenuToolBarImages.Images.SetKeyName(47, "Find Dictionary.png");
			this.teMenuToolBarImages.Images.SetKeyName(48, "CheckErrorIsIrrelevant-gray.bmp");
			this.teMenuToolBarImages.Images.SetKeyName(49, "Find Keyterm.bmp");
			//
			// m_teSideBarTabImages
			//
			this.m_teSideBarTabImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_teSideBarTabImages.ImageStream")));
			this.m_teSideBarTabImages.TransparentColor = System.Drawing.Color.Magenta;
			this.m_teSideBarTabImages.Images.SetKeyName(0, "");
			this.m_teSideBarTabImages.Images.SetKeyName(1, "");
			this.m_teSideBarTabImages.Images.SetKeyName(2, "");
			this.m_teSideBarTabImages.Images.SetKeyName(3, "");
			this.m_teSideBarTabImages.Images.SetKeyName(4, "");
			this.m_teSideBarTabImages.Images.SetKeyName(5, "");
			this.m_teSideBarTabImages.Images.SetKeyName(6, "");
			this.m_teSideBarTabImages.Images.SetKeyName(7, "");
			//
			// TeResourceHelper
			//
			resources.ApplyResources(this, "$this");
			this.Name = "TeResourceHelper";
			this.ResumeLayout(false);

		}
		#endregion

		#region Static members
		private static TeResourceHelper TeResHelper
		{
			get
			{
				// Note: this doesn't work if we have more than one class that
				// derives from ResourceHelper! The reason we do it this way is
				// that we can gracefully dispose of the resource helper in
				// the unit tests to prevent hanging tests (FWNX-455).
				if (!(s_form is TeResourceHelper))
				{
					// Make sure we have the right kind of resource helper
					if (s_form != null)
						s_form.Dispose();
					s_form = new TeResourceHelper();
				}
				return (TeResourceHelper)s_form;
			}
		}

		/// <summary>
		/// Shut down the TEResourceHelper Form.
		/// </summary>
		[Obsolete("Use Resource.ShutdownHelper instead")]
		public static void ShutDownTEResourceHelper()
		{
			ShutdownHelper();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Function to create appropriate labels for Undo tasks, with the action names coming
		/// from the stid.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="stUndo">Returns string for Undo task</param>
		/// <param name="stRedo">Returns string for Redo task</param>
		/// -----------------------------------------------------------------------------------
		public new static void MakeUndoRedoLabels(string stid, out string stUndo,
			out string stRedo)
		{
			string stRes = GetResourceString(stid);

			// If we get here from a test, it might not find the correct resource.
			// Just ignore it and set some dummy values
			if (string.IsNullOrEmpty(stRes))
			{
				ResourceHelper.MakeUndoRedoLabels(stid, out stUndo, out stRedo);
				return;
			}
			string[] stStrings = stRes.Split('\n');
			if (stStrings.Length > 1)
			{
				// The resource string contains two separate strings separated by a new-line.
				// The first half is for Undo and the second for Redo.
				stUndo = stStrings[0];
				stRedo = stStrings[1];
			}
			else
			{
				// Insert the string (describing the task) into the undo/redo frames.
				stUndo =
					string.Format(ResourceHelper.GetResourceString("kstidUndoFrame"), stRes);
				stRedo =
					string.Format(ResourceHelper.GetResourceString("kstidRedoFrame"), stRes);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static new string GetResourceString(string stid)
		{
			if (s_stringResources == null)
			{
				s_stringResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.TE.TeStrings", Assembly.GetExecutingAssembly());
			}
			string toReturn = (stid == null ? "NullStringID" : s_stringResources.GetString(stid));
			if (toReturn == null)
				toReturn = DlgResources.ResourceString(stid);

			return toReturn;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID, with formatting placeholders replaced by the
		/// supplied parameters.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <param name="parameters">zero or more parameters to format the resource string</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static new string FormatResourceString(string stid, params object[] parameters)
		{
			return String.Format(GetResourceString(stid), parameters);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a help topic or help file path.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static new string GetHelpString(string stid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.TE.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_helpResources.GetString(stid));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID for a toolbar/menu item.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static string GetTmResourceString(string stid)
		{
			if (s_toolbarResources == null)
			{
				s_toolbarResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.TE.TeTMStrings", Assembly.GetExecutingAssembly());
			}
			return stid == null ? "NullStringID" : s_toolbarResources.GetString(stid);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Guid for the TE application (used for uniquely identifying DB items that "belong"
		/// to TE).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid TeAppGuid
		{
			get	{return new Guid("A7D421E1-1DD3-11d5-B720-0010A4B54856");}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an imagelist with all the large sidebar bitmaps (these are 32x32 images).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ImageList TeSideBarTabImages
		{
			get {return TeResHelper.m_teSideBarTabImages;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an imagelist with all the large sidebar bitmaps (these are 32x32 images).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ImageList TeSideBarLargeImages
		{
			get {return TeResHelper.m_teSideBarLargeImages;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an imagelist with all the small sidebar bitmaps (these are 16x16 images).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ImageList TeSideBarSmallImages
		{
			get {return TeResHelper.m_teSideBarSmallImages;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the Back Translation Unfinished icon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BackTranslationUnfinishedImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[9];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the Back Translation Finished icon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BackTranslationFinishedImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[8];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the Back Translation Checked icon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image BackTranslationCheckedImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[10];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image associated with an ignored inconsistency.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image IgnoredInconsistency
		{
			get { return TeResHelper.teMenuToolBarImages.Images["IgnoredInconsistency"]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image associated with an ignored inconsistency.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image UnignoredInconsistency
		{
			get { return TeResHelper.teMenuToolBarImages.Images["UnignoredInconsistency"]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for rendered and explicitly assigned key terms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image KeyTermRenderedImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[14];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for applying the filter to key terms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image KeyTermFilterImage
		{
			get { return TeResHelper.teMenuToolBarImages.Images[36]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for displaying the Find Key Term control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image FindKeyTermImage
		{
			get { return TeResHelper.teMenuToolBarImages.Images[49]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for automatically assigned key terms (automatically assigned
		/// because the verse with a key term includes the same rendering as an explicitly
		/// rendered key term).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image KeyTermAutoAssignedImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[16];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for unrendered keyterms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image KeyTermNotRenderedImage
		{
			get { return TeResHelper.teMenuToolBarImages.Images[15]; }
		}

		/// <summary>
		/// Retrieve the icon for keyterms missing rendered keyterm
		/// </summary>
		public static Image KeyTermMissingRenderedImage
		{
			get { return TeResHelper.teMenuToolBarImages.Images["KeyTermMissingRenderedImage"]; }
		}

		/// <summary>
		/// Retrieve the icon for updating the renderings for keyterms
		/// </summary>
		public static Image UpdateKeyTermEquivalentsImage
		{
			get { return TeResHelper.teMenuToolBarImages.Images["UpdateKeyTermEquivalentsImage"]; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the icon for ignored key terms.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image KeyTermIgnoreRenderingImage
		{
			get {return TeResHelper.teMenuToolBarImages.Images[13];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image used when a checking error becomes irrelevant.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Image CheckErrorIrrelevant
		{
			get {return TeResHelper.teMenuToolBarImages.Images[48];}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the InsertVerseCursor (loads it from resources if necessary)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Cursor InsertVerseCursor
		{
			get
			{
				if (s_insertVerseCursor == null)
				{
					try
					{
						// Read cursor from embedded resource
						Assembly assembly = Assembly.GetAssembly(typeof(TeResourceHelper));
						using (System.IO.Stream stream = assembly.GetManifestResourceStream(
							"SIL.FieldWorks.TE.INS_VRSE_NBRS.CUR"))
						{
							s_insertVerseCursor = new Cursor(stream);
						}
					}
					catch
					{
						s_insertVerseCursor = Cursors.Cross;
					}
				}
				return s_insertVerseCursor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the RightCursor (loads it from resources if necessary)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Cursor RightCursor
		{
			get
			{
				if (s_rightCursor == null)
				{
					try
					{
						// Read cursor from embedded resource
						Assembly assembly = Assembly.GetAssembly(typeof(TeResourceHelper));
						using (System.IO.Stream stream = assembly.GetManifestResourceStream(
							"SIL.FieldWorks.TE.RightArrow.cur"))
						{
							s_rightCursor = new Cursor(stream);
						}
					}
					catch
					{
						s_rightCursor = Cursors.Arrow;
					}
				}
				return s_rightCursor;
			}
		}
		#endregion
	}
}
