namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An interface which indicates that the control may display values inherited from a parent setting
	/// </summary>
	public interface IInheritableFwControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A value indicating whether this instance represents a property which is
		/// inherited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsInherited
		{
			get;
			set;
		}
	}
}
