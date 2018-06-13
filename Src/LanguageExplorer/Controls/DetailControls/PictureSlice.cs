// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A slice that displays a picture (CmPicture).
	/// </summary>
	internal class PictureSlice: Slice
	{
		readonly ICmPicture m_picture;
		Size m_lastSize = new Size(0, 0);
		float m_aspectRatio; // ideal aspect ratio for picture (height/width).
		PictureBox m_picBox;
		bool m_fThumbnail; // true to force smaller size.

		/// <summary />
		public PictureSlice(ICmPicture picture)
		{
			m_picBox = new PictureBox();
			m_picBox.Click += pb_Click;
			m_picBox.Location = new Point(0,0); // not docked, because width may not be whole width
			m_picBox.SizeMode = PictureBoxSizeMode.Zoom;
			m_picture = picture;
			InstallPicture(m_picBox);
			// We need an extra layer of panel because the slice's control is always docked,
			// and we don't want that for the picture box.
			var panel = new Panel();
			panel.Controls.Add(m_picBox);
			panel.SizeChanged += panel_SizeChanged;
			Control = panel;
		}

		// Read the thumbnail property from the configuration
		public override void FinishInit()
		{
			base.FinishInit ();
			m_fThumbnail = XmlUtils.GetOptionalBooleanAttributeValue(ConfigurationNode, "thumbnail", false);
		}

		private void InstallPicture(PictureBox pb)
		{
			try
			{
				pb.Image = Image.FromFile(FileUtils.ActualFilePath(m_picture.PictureFileRA.AbsoluteInternalPath));
				m_aspectRatio = pb.Image.Height / (float)pb.Image.Width;
				if (m_aspectRatio == 0.0)
				{
					m_aspectRatio = 0.0001F; // avoid divide by zero.
				}
			}
			catch
			{
				// If we can't get the image for some reason, just ignore?
			}
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.

				// Prevents memory leaks and also spurious continued locking of the file.
				if (m_picBox?.Image != null)
				{
					m_picBox.Image.Dispose();
					m_picBox.Image = null;
				}
				m_picBox = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override void OnSizeChanged(EventArgs e)
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
			{
				return;
			}

			base.OnSizeChanged (e);

			if (Control.Size == m_lastSize)
			{
				return;
			}
			m_lastSize = Control.Size;
			var image = m_picBox.Image;
			if (image == null || image.Width == 0)
			{
				Height = LabelHeight;
			}
			var idealHeight = (int)(m_aspectRatio * Control.Width);
			var height = Math.Min(idealHeight, ContainingDataTree.Height / 3);
			if (m_fThumbnail && height > 80)
			{
				height = 80;
			}

			Height = Math.Max(LabelHeight, height);
			m_picBox.Height = Height;
			if (Control.Height != Height)
			{
				Control.Height = Height;
			}
			if (height < idealHeight)
			{
				m_picBox.Width = (int)(Control.Height / m_aspectRatio);
			}
			else
			{
				m_picBox.Width = Control.Width;
			}
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick (e);
			showProperties();
		}

		public void showProperties()
		{
			var pic = (ICmPicture)MyCmObject;
			var app = PropertyTable.GetValue<IApp>("App");
			using (var dlg = new PicturePropertiesDialog(Cache, pic, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, true))
			{
				if (!dlg.Initialize())
				{
					return;
				}
				var stylesheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
				dlg.UseMultiStringCaption(Cache, WritingSystemServices.kwsVernAnals, stylesheet);
				dlg.SetMultilingualCaptionValues(pic.Caption);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoUpdatePicture, DetailControlsStrings.ksRedoUpdatePicture, MyCmObject, () =>
					{
						const string strLocalPictures = CmFolderTags.DefaultPictureFolder;
						dlg.GetMultilingualCaptionValues(pic.Caption);
						pic.UpdatePicture(dlg.CurrentFile, null, strLocalPictures, 0);
					});
					InstallPicture(m_picBox);
					m_lastSize = new Size(0, 0); // forces OnSizeChanged to do something (we need to adjust to new aspect ratio).
					OnSizeChanged(new EventArgs());
				}
			}
		}

		private void pb_Click(object sender, EventArgs e)
		{
			OnClick(e);
		}

		private void panel_SizeChanged(object sender, EventArgs e)
		{
			OnSizeChanged(e);
		}
	}
}