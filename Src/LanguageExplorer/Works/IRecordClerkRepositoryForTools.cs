using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// Interface that language Explorer tools use to create clerks when they are not in the main clerk repository.
	/// </summary>
	/// <remarks>
	/// Clients that are not tools should never use this interface.
	/// </remarks>
	internal interface IRecordClerkRepositoryForTools : IRecordClerkRepository
	{
		/// <summary>
		/// Get a clerk with the given <paramref name="clerkId"/>, creating one, if needed using <paramref name="clerkFactoryMethod"/>.
		/// </summary>
		/// <param name="clerkId">The clerk Id to return.</param>
		/// <param name="statusBar"></param>
		/// <param name="clerkFactoryMethod">The method called to create the clerk, if not found in the repository.</param>
		/// <returns>A RecordClerk instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="clerkFactoryMethod"/> doesn't know how to make a clerk with the given Id.</exception>
		RecordClerk GetRecordClerk(string clerkId, StatusBar statusBar, Func<LcmCache, FlexComponentParameters, string, StatusBar, RecordClerk> clerkFactoryMethod);
	}
}