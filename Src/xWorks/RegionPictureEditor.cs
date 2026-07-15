// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.Windows.Forms.ImageToolbox;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19d: the ONE LCModel-backed picture write path shared by the composed edit context
	/// (<see cref="ComposedRegionEditContext"/>) and the picture-properties dialog's apply step. Each
	/// method mutates the domain DIRECTLY (the caller wraps it in the fenced edit session so it rides
	/// one undoable step); the picture metadata exchange uses the LCModel-free
	/// <see cref="RegionPictureMetadata"/> so the FwAvalonia view never sees LCModel.
	/// Mirrors legacy <c>DTMenuHandler.OnInsertPicture</c> / <c>PictureSlice.showProperties</c> /
	/// <c>FwEditingHelper.InsertPicture</c> but without WinForms, so the same gestures work on the
	/// Avalonia surface. The caption/description are real <c>ICmPicture</c> multistring properties; the
	/// license/creator are applied to the image file's metadata only when a real, writable file exists
	/// (an in-memory test cache with no file simply skips them — the LCModel round-trip still holds).
	/// </summary>
	internal static class RegionPictureEditor
	{
		/// <summary>
		/// Creates a new <see cref="ICmPicture"/> in the vector property <paramref name="flid"/> of
		/// <paramref name="owner"/> from <paramref name="sourceFile"/>, applying <paramref name="metadata"/>.
		/// Returns the created picture, or null when the source file is missing.
		/// </summary>
		public static ICmPicture CreatePicture(LcmCache cache, ICmObject owner, int flid, string sourceFile,
			RegionPictureMetadata metadata)
		{
			if (cache == null || owner == null || flid == 0)
				return null;
			if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
				return null;

			var chvo = cache.DomainDataByFlid.get_VecSize(owner.Hvo, flid);
			var hvoPic = cache.DomainDataByFlid.MakeNewObject(CmPictureTags.kClassId, owner.Hvo, flid, chvo);
			var picture = cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(hvoPic);
			// UpdatePicture copies/links the file into the project's Pictures folder, like the legacy insert.
			picture.UpdatePicture(sourceFile, CaptionTss(cache, metadata), CmFolderTags.DefaultPictureFolder, 0);
			ApplyModelMetadata(cache, picture, metadata);
			ApplyFileMetadata(picture, metadata);
			return picture;
		}

		/// <summary>Replaces the image file of an existing picture, leaving its caption/metadata intact.</summary>
		public static bool ReplaceFile(LcmCache cache, ICmPicture picture, string sourceFile)
		{
			if (cache == null || picture == null)
				return false;
			if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
				return false;
			picture.UpdatePicture(sourceFile, picture.Caption?.BestAnalysisVernacularAlternative,
				CmFolderTags.DefaultPictureFolder, 0);
			return true;
		}

		/// <summary>Deletes the picture from its owning collection.</summary>
		public static bool Delete(ICmPicture picture)
		{
			if (picture == null || !picture.IsValidObject)
				return false;
			picture.Delete();
			return true;
		}

		/// <summary>
		/// Applies caption/description (LCModel) and license/creator (file metadata, best effort) to an
		/// existing picture.
		/// </summary>
		public static bool SetMetadata(LcmCache cache, ICmPicture picture, RegionPictureMetadata metadata)
		{
			if (cache == null || picture == null || !picture.IsValidObject)
				return false;
			ApplyModelMetadata(cache, picture, metadata);
			ApplyFileMetadata(picture, metadata);
			return true;
		}

		/// <summary>
		/// §19d closes §19c's picture-ORC deferral: creates a "loose" picture (owned by the project's
		/// pictures collection, like legacy <c>FwEditingHelper.InsertPicture</c>) from
		/// <paramref name="sourceFile"/> and inserts its <c>kodtGuidMoveableObjDisp</c> ORC into
		/// <paramref name="tss"/> at <paramref name="caretPosition"/>, returning the new TsString (with the
		/// ORC run). Returns null when the file is missing. The caller writes the returned TsString back to
		/// the field; the guid/ORC encoding is done by <c>ICmPicture.InsertORCAt</c> (the canonical path),
		/// so the view never hand-encodes ObjectData.
		/// </summary>
		public static ITsString InsertPictureOrc(LcmCache cache, ITsString tss, int caretPosition,
			string sourceFile, RegionPictureMetadata metadata)
		{
			if (cache == null || tss == null)
				return null;
			if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
				return null;

			// The factory overload that takes a file creates the CmPicture AND places it in the named
			// CmFolder under the project's pictures collection (the same path legacy
			// FwEditingHelper.InsertPicture uses), so we never touch PicturesOC (a folder collection) directly.
			var factory = cache.ServiceLocator.GetInstance<ICmPictureFactory>();
			var picture = factory.Create(sourceFile, CaptionTss(cache, metadata), CmFolderTags.DefaultPictureFolder);
			ApplyModelMetadata(cache, picture, metadata);
			ApplyFileMetadata(picture, metadata);

			var ich = Math.Max(0, Math.Min(caretPosition, tss.Length));
			return picture.InsertORCAt(tss, ich);
		}

		/// <summary>Projects an existing picture's editable metadata into the LCModel-free DTO (for seeding the dialog).</summary>
		public static RegionPictureMetadata ReadMetadata(ICmPicture picture)
		{
			if (picture == null || !picture.IsValidObject)
				return new RegionPictureMetadata();
			var caption = picture.Caption?.BestAnalysisVernacularAlternative?.Text;
			var description = picture.Description?.BestAnalysisVernacularAlternative?.Text;
			string license = null;
			string creator = null;
			try
			{
				var path = picture.PictureFileRA?.AbsoluteInternalPath;
				if (!string.IsNullOrEmpty(path) && File.Exists(path))
				{
					using (var palasoImage = PalasoImage.FromFile(path))
					{
						license = palasoImage?.Metadata?.CopyrightNotice;
						creator = palasoImage?.Metadata?.Creator;
					}
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine($"RegionPictureEditor.ReadMetadata: file metadata unread: {e.Message}");
			}
			return new RegionPictureMetadata(caption, description, license, creator);
		}

		private static void ApplyModelMetadata(LcmCache cache, ICmPicture picture, RegionPictureMetadata metadata)
		{
			if (metadata == null)
				return;
			var anal = cache.DefaultAnalWs;
			if (metadata.Caption != null)
				picture.Caption.set_String(anal, TsStringUtils.MakeString(metadata.Caption, anal));
			if (metadata.Description != null)
				picture.Description.set_String(anal, TsStringUtils.MakeString(metadata.Description, anal));
		}

		// License/creator are stored in the image FILE's Palaso metadata (not LCModel), so they can only be
		// applied to a real, writable file. An in-memory test cache (no real file) simply skips this; the
		// caption/description LCModel writes still round-trip. Best-effort: never throw out of an edit.
		private static void ApplyFileMetadata(ICmPicture picture, RegionPictureMetadata metadata)
		{
			if (metadata == null || (metadata.License == null && metadata.Creator == null))
				return;
			try
			{
				var path = picture.PictureFileRA?.AbsoluteInternalPath;
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
					return;
				using (var palasoImage = PalasoImage.FromFile(path))
				{
					if (palasoImage?.Metadata == null)
						return;
					if (metadata.Creator != null)
						palasoImage.Metadata.Creator = metadata.Creator;
					if (metadata.License != null)
						palasoImage.Metadata.CopyrightNotice = metadata.License;
					palasoImage.SaveUpdatedMetadataIfItMakesSense();
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine($"RegionPictureEditor.ApplyFileMetadata: skipped ({e.Message}).");
			}
		}

		private static ITsString CaptionTss(LcmCache cache, RegionPictureMetadata metadata)
		{
			if (metadata == null || string.IsNullOrEmpty(metadata.Caption))
				return null;
			return TsStringUtils.MakeString(metadata.Caption, cache.DefaultAnalWs);
		}
	}
}
