using System;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary></summary>
	[TestFixture]
	public class RecentItemsCacheTests // can't derive from BaseTest because of dependencies
	{
		private RecentItemsCache<string, int> m_recentItems;

		/// <summary></summary>
		[Test]
		public void RetrieveItems_algorithmTest()
		{
			m_recentItems = new RecentItemsCache<string, int>(5);
			AddNewItem("1", 6);
			AddNewItem("2", 5);
			AddNewItem("3", 4);
			AddNewItem("4", 3);
			AddNewItem("5", 2);
			// At this point the cache is fully loaded. When we add another, it will have to discard
			// at least the least frequently requested item.
			AddNewItem("6", 1);
			// So, when we add "5" again, it will have to be recomputed.
			AddNewItem("5", 2);
			// The question is, which one should it have discarded in order to add "5" again?
			// "6" is plausible, since it has only been asked for once, while all the other
			// values have been requested more. However, we want some bias towards things
			// recently asked for. We'd like to see "4" as the next one chosen to remove.
			AddNewItem("4", 2);
			// Don't want to constrain the algorithm too closely, but after several cycles even "1" should be discarded.
			AddNewItem("7", 2);
			AddNewItem("8", 2);
			AddNewItem("9", 2);
			AddNewItem("10", 2);
			AddNewItem("11", 2);
			AddNewItem("1", 1);
			AddItem("11", false); // most recent one should still be around.
		}

		/// <summary>
		/// Add key,value pair to cache, and repeatedly request it to build up its frequency-of-request table.
		/// key must not yet be in the cache.
		/// </summary>
		private void AddNewItem(string key, int count)
		{
			AddItem(key, true);
			for (int i = 1; i < count; i++)
				AddItem(key, false);
		}

		private void AddItem(string key, bool expectToCompute)
		{
			AddItem<string, int>(m_recentItems, key,
				inputKey => { return int.Parse(inputKey); }, expectToCompute);
		}

		/// <summary>
		/// Add key,value pair to cache, checking if it was newly added.
		/// Will assert if computed if not expected, or did not compute if expected to.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="key"></param>
		/// <param name="creator">Delegate to create the value from key</param>
		/// <param name="expectToCompute">
		/// Whether should expect to need to compute the value from key using creator
		/// </param>
		private void AddItem<K,V>(RecentItemsCache<K,V> cache, K key, Func<K,V> creator,
			bool expectToCompute)
		{
			bool didCompute = false;
			Assert.AreEqual(creator(key), cache.GetItem(key, key1 =>
				{
					didCompute = true;
					return creator(key1);
				}), String.Format("wrong value for {0} cached key given", didCompute?"newly":"previously"));

			string errorMessage;
			if (expectToCompute)
				errorMessage = "Expected to compute value from key since it was not expected to be in the cache, but it was in the cache.";
			else
				errorMessage = "Did not expect to compute value from key since expected it to be in the cache, but it was not in the cache.";
			Assert.AreEqual(expectToCompute, didCompute, errorMessage);
		}

		private void AddNewItem<K,V>(RecentItemsCache<K,V> cache, K key, Func<K,V> creator)
		{
			AddItem<K, V>(cache, key, creator, true);
		}

		private void GetExistingItem<K,V>(RecentItemsCache<K,V> cache, K key, Func<K,V> creator)
		{
			AddItem<K, V>(cache, key, creator, false);
		}

		/// <summary></summary>
		[Test]
		public void Cache_addItem()
		{
			Func<int, string> creator = (int key) => {
				return key.ToString();
			};

			var cache = new RecentItemsCache<int, string>(7);
			AddNewItem<int, string>(cache, 0, creator);
		}

		/// <summary></summary>
		[Test]
		public void Cache_retainsItem()
		{
			Func<int, string> creator = (int key) => {
				return key.ToString();
			};

			var cache = new RecentItemsCache<int, string>(7);
			AddNewItem<int, string>(cache, 0, creator);
			GetExistingItem<int, string>(cache, 0, creator);
		}

		/// <summary></summary>
		[Test]
		public void Cache_retainsManyItems()
		{
			Func<int, string> creator = (int key) => {
				return key.ToString();
			};

			int capacity = 7;
			var cache = new RecentItemsCache<int, string>(capacity);
			for (int i = 0; i < capacity; i++)
				AddNewItem<int, string>(cache, i, creator);
			for (int i = 0; i < capacity; i++)
				GetExistingItem<int, string>(cache, i, creator);
		}

		/// <summary></summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullInput_key()
		{
			// Attempt to cache a key,value pair with null key
			var cache = new RecentItemsCache<string, string>(7);
			cache.GetItem(null, key => { return "value"; });
		}

		/// <summary></summary>
		[Test]
		public void NullInput_value()
		{
			// Cache a key,value pair with null value.
			var cache = new RecentItemsCache<int, string>(7);
			Assert.DoesNotThrow(() => {
				cache.GetItem(0, key => { return null; });
				}, "Null value should not throw exception");
		}

		/// <summary></summary>
		[Test]
		public void Caching_itemsFallOutOfCache()
		{
			Func<int, string> creator = (int key) => {
				return key.ToString();
			};

			var cacheCapacity = 7;
			var cache = new RecentItemsCache<int, string>(cacheCapacity);
			int valueLargerThanCacheCapacity = cacheCapacity * 3;

			// Add some items, all of which should be new to the cache
			for (int i = 0; i < valueLargerThanCacheCapacity; i++)
				AddNewItem<int, string>(cache, i, creator);

			// Adding the first few items again should not be able to retrieve them from the cache.
			// They should have fallen out of the cache by now.
			for (int i = 0; i < cacheCapacity; i++)
				AddNewItem<int, string>(cache, i, creator);
		}

		/// <summary></summary>
		class MyKey : IEquatable<MyKey>
		{
			/// <summary></summary>
			public string Data { get; set; }

			/// <summary></summary>
			public MyKey(int data)
			{
				Data = data.ToString();
			}

			/// <summary></summary>
			public override int GetHashCode()
			{
				return Data.GetHashCode();
			}

			#region IEquatable implementation
			/// <summary></summary>
			public bool Equals(MyKey other)
			{
				return this.Data == other.Data;
			}
			#endregion
		}

		/// <summary></summary>
		[Test]
		public void Cache_matchesKeyOfSameReference()
		{
			Func<MyKey, string> creator = (MyKey inputKey) => {
				return inputKey.Data + "value";
			};

			int capacity = 7;
			var cache = new RecentItemsCache<MyKey, string>(capacity);

			// Keys have the same Reference
			var key1 = new MyKey(0);
			var key2 = key1;

			AddNewItem<MyKey, string>(cache, key1, creator);
			GetExistingItem<MyKey, string>(cache, key2, creator);
		}

		/// <summary></summary>
		[Test]
		public void Cache_matchesEqualKeyOfDifferentReference()
		{
			Func<MyKey, string> creator = (MyKey inputKey) => {
				return inputKey.Data + "value";
			};

			int capacity = 7;
			var cache = new RecentItemsCache<MyKey, string>(capacity);

			// Two keys: equal in data/value but of different references
			var key1 = new MyKey(0);
			var key2 = new MyKey(0);

			AddNewItem<MyKey, string>(cache, key1, creator);
			GetExistingItem<MyKey, string>(cache, key2, creator);
		}

		/// <summary></summary>
		[Test]
		public void Cache_reportsIfItemWasRetrievedFromCache()
		{
			Func<int, string> creator = key => {
				return key.ToString();
			};

			bool wasRetrievedFromCache;
			var cache = new RecentItemsCache<int, string>(7);
			cache.GetItem(0, creator, out wasRetrievedFromCache);
			Assert.AreEqual(false, wasRetrievedFromCache, "Adding a new item to cache should not report it as retrieved from cache.");
			cache.GetItem(0, creator, out wasRetrievedFromCache);
			Assert.AreEqual(true, wasRetrievedFromCache, "Retrieving a cached item should report that it was retrieved from cache.");
		}
	}
}
