// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using SIL.Reporting;
using SIL.Windows.Forms.ClearShare;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dictionary Configuration Model Extensions
	/// When we need to generate model related content for a field that can't be added through a Property in LCM
	/// due to design constraints an extension method in this class can be used to provide a property like Get method.
	/// The extension methods must take no parameters and return either a primitive or a LCM type.
	/// </summary>
	internal static class DictConfigModelExt
	{
		public static string Creator(this LCModel.ICmPicture picture)
		{
			return picture.MetadataFromFile()?.Creator;
		}

		public static string CopyrightAndLicense(this LCModel.ICmPicture picture)
		{
			var metadata = picture.MetadataFromFile();
			if (metadata == null)
			{
				return null;
			}
			// As of 2023.07, the only implementation that actually uses the language list is CustomLicense w/o custom text,
			// which our UI seems to prevent users from creating.
			var license = metadata.License?.GetMinimalFormForCredits(new[] { "en" }, out _);
			if (string.IsNullOrEmpty(metadata.CopyrightNotice) && string.IsNullOrEmpty(license))
				return null;
			// We want the short copyright notice, but it isn't safe to ask for if CopyrightNotice is null
			var copyright = string.IsNullOrEmpty(metadata.CopyrightNotice)
				? string.Empty
				: metadata.ShortCopyrightNotice;
			return string.Join(", ", new[] { copyright, license }.Where(txt => !string.IsNullOrEmpty(txt)));
		}

		private static Metadata MetadataFromFile(this LCModel.ICmPicture picture)
		{
			var path = picture.PictureFileRA?.AbsoluteInternalPath;
			try
			{
				return Metadata.FromFile(path);
			}
			catch (Exception e)
			{
				Logger.WriteError($"Error getting metadata from {path}", e);
				return null;
			}
		}
	}
}
