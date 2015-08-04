namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface for major FLEx components
	/// </summary>
	public interface IMajorFlexUiComponent : IMajorFlexComponent
	{
		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		string MachineName { get; }

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		string UiName { get; }
	}
}