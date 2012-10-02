using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Writing System utilities
	/// </summary>
	public static class WritingSystemUtils
	{
		/// <summary>
		/// Gets all writing systems that are related to the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="wss">The writing systems.</param>
		/// <returns></returns>
		public static IEnumerable<IWritingSystem> Related(this IEnumerable<IWritingSystem> wss, IWritingSystem ws)
		{
			return wss.Where(curWs => ws.LanguageSubtag == curWs.LanguageSubtag && curWs != ws);
		}

		/// <summary>
		/// Returns the handles for the specified writing systems.
		/// </summary>
		/// <param name="wss">The writing systems.</param>
		/// <returns></returns>
		public static IEnumerable<int> Handles(this IEnumerable<IWritingSystem> wss)
		{
			return wss.Select(ws => ws.Handle);
		}

		/// <summary>
		/// Determines whether the writing systems contains a writing system with the specified handle.
		/// </summary>
		/// <param name="wss">The writing systems.</param>
		/// <param name="wsHandle">The writing system handle.</param>
		/// <returns>
		/// 	<c>true</c> if the writing systems contains a writing system with the handle, otherwise <c>false</c>.
		/// </returns>
		public static bool Contains(this IEnumerable<IWritingSystem> wss, int wsHandle)
		{
			return wss.Any(ws => ws.Handle == wsHandle);
		}

		/// <summary>
		/// Gets all distinct writing systems (local and global) from the writing system manager. Local writing systems
		/// take priority over global writing systems.
		/// </summary>
		/// <param name="wsManager">The ws manager.</param>
		/// <returns></returns>
		public static IEnumerable<IWritingSystem> GetAllDistinctWritingSystems(IWritingSystemManager wsManager)
		{
			return wsManager.LocalWritingSystems.Concat(wsManager.GlobalWritingSystems.Except(wsManager.LocalWritingSystems,
				new WsIdEqualityComparer()));
		}
	}

	/// <summary>
	/// An equality comparer for writing systems that determines equality based on the writing system identifier.
	/// </summary>
	public class WsIdEqualityComparer : IEqualityComparer<IWritingSystem>
	{
		/// <summary>
		/// Determines whether the specified writing systems are equal.
		/// </summary>
		/// <returns>
		/// true if the specified writing systems are equal; otherwise, false.
		/// </returns>
		/// <param name="x">The first writing system to compare.</param>
		/// <param name="y">The second writing system to compare.</param>
		public bool Equals(IWritingSystem x, IWritingSystem y)
		{
			if (ReferenceEquals(x, y))
				return true;

			return x.Id == y.Id;
		}

		/// <summary>
		/// Returns a hash code for the specified writing system.
		/// </summary>
		/// <returns>
		/// A hash code for the specified writing system.
		/// </returns>
		/// <param name="ws">The writing system for which a hash code is to be returned.</param>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="ws"/> is null.</exception>
		public int GetHashCode(IWritingSystem ws)
		{
			return ws.Id.GetHashCode();
		}
	}
}
