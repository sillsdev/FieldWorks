// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
		   return Metadata.FromFile(picture.PictureFileRA.AbsoluteInternalPath).Creator;
	  }
	   public static string CopyrightAndLicense(this LCModel.ICmPicture picture)
	   {
		   var metadata = Metadata.FromFile(picture.PictureFileRA.AbsoluteInternalPath);
		   // As of 2023.07, the only implementation that actually uses the language list is CustomLicense w/o custom text,
		   // which our UI seems to prevent users from creating.
		   var license = metadata.License.GetMinimalFormForCredits(new[] { "en" }, out _);
		   return string.IsNullOrEmpty(license) ? metadata.ShortCopyrightNotice : string.Join(", ", metadata.ShortCopyrightNotice, license);
	   }
   }
}
