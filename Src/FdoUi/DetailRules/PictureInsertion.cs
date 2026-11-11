// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Insert Picture command of a detail row (message InsertPicture): which field of the
	/// row's object takes the picture, and the picture it gets. Choosing the file stays with
	/// the caller.
	/// </summary>
	public static class PictureInsertion
	{
		/// <summary>
		/// The id of the field that takes a picture inserted on <paramref name="target"/>:
		/// <paramref name="fieldName"/> when the target is exactly of class
		/// <paramref name="className"/>, not a subclass; 0 otherwise, and when the model lacks
		/// the class or the field.
		/// </summary>
		public static int TargetField(ICmObject target, string className, string fieldName)
		{
			if (target == null || !target.IsValidObject || string.IsNullOrEmpty(className) || string.IsNullOrEmpty(fieldName))
				return 0;
			var mdc = (IFwMetaDataCacheManaged)target.Cache.MetaDataCacheAccessor;
			if (!mdc.ClassExists(className))
				return 0;
			var classId = mdc.GetClassId(className);
			if (target.ClassID != classId || !mdc.FieldExists(classId, fieldName, true))
				return 0;
			return mdc.GetFieldId2(classId, fieldName, true);
		}

		/// <summary>
		/// Adds a picture of <paramref name="file"/> at the end of field
		/// <paramref name="flid"/> of <paramref name="owner"/>, in one undoable step.
		/// </summary>
		/// <returns>The new picture.</returns>
		public static ICmPicture Insert(ICmObject owner, int flid, string file, string undoText, string redoText)
		{
			ICmPicture picture = null;
			var sda = owner.Cache.DomainDataByFlid;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, owner, () =>
			{
				var hvo = sda.MakeNewObject(CmPictureTags.kClassId, owner.Hvo, flid, sda.get_VecSize(owner.Hvo, flid));
				picture = owner.Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(hvo);
				picture.UpdatePicture(file, null, CmFolderTags.DefaultPictureFolder, 0);
			});
			return picture;
		}
	}
}
