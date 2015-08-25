using SIL.CoreImpl;
using SIL.FieldWorks.FDO;

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
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		void Init(FdoCache cache, IPropertyTable propertyTable, IPublisher publisher);
	}
}
