using System.Collections.Generic;
using System.Linq;

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
		public static IEnumerable<WritingSystem> Related(this IEnumerable<WritingSystem> wss, WritingSystem ws)
		{
			return wss.Where(curWs => AreRelated(ws, curWs));
		}

		private static bool AreRelated(WritingSystem ws1, WritingSystem ws2)
		{
			return ws1.Language == ws2.Language && ws2 != ws1;
		}

		/// <summary>
		/// Returns the handles for the specified writing systems.
		/// </summary>
		/// <param name="wss">The writing systems.</param>
		/// <returns></returns>
		public static IEnumerable<int> Handles(this IEnumerable<WritingSystem> wss)
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
		public static bool Contains(this IEnumerable<WritingSystem> wss, int wsHandle)
		{
			return wss.Any(ws => ws.Handle == wsHandle);
		}
	}

	/// <summary>
	/// An equality comparer for writing systems that determines equality based on the writing system identifier.
	/// </summary>
	public class WritingSystemIDEqualityComparer : IEqualityComparer<WritingSystem>
	{
		/// <summary>
		/// Determines whether the specified writing systems are equal.
		/// </summary>
		/// <returns>
		/// true if the specified writing systems are equal; otherwise, false.
		/// </returns>
		/// <param name="x">The first writing system to compare.</param>
		/// <param name="y">The second writing system to compare.</param>
		public bool Equals(WritingSystem x, WritingSystem y)
		{
			if (ReferenceEquals(x, y))
				return true;

			return x.ID == y.ID;
		}

		/// <summary>
		/// Returns a hash code for the specified writing system.
		/// </summary>
		/// <returns>
		/// A hash code for the specified writing system.
		/// </returns>
		/// <param name="ws">The writing system for which a hash code is to be returned.</param>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="ws"/> is null.</exception>
		public int GetHashCode(WritingSystem ws)
		{
			return ws.ID.GetHashCode();
		}
	}
}
