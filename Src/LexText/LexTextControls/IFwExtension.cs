using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// A small interface to allow for plugin stuff, like import dialogs
	/// </summary>
	public interface IFwExtension
	{
		/// <summary>
		/// Called instead of a constructor with parameters
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		void Init(FdoCache cache, Mediator mediator, PropertyTable propertyTable);
	}
}
