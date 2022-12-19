using System;
using System.Collections;
using System.Collections.Generic;

namespace Dna.DataStructures
{
	[Serializable]
	public class KeyedList<K, V> : IDictionary<K, V>, IList<KeyValuePair<K, V>>
	{
		private Dictionary<K, V> objectTable = new Dictionary<K, V>();
		private List<KeyValuePair<K, V>> objectList = new List<KeyValuePair<K, V>>();

		/// <summary>
		/// Returns false.
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Returns the number of entries in the KeyedList.
		/// </summary>
		public int Count
		{
			get { return objectList.Count; }
		}

		/// <summary>
		/// Get/Set the value at the specified index.
		/// </summary>
		/// <param name="idx">The index.</param>
		/// <returns>The value.</returns>
		public KeyValuePair<K, V> this[int idx]
		{
			get
			{
				if (idx < 0 || idx >= Count)
				{
					throw new ArgumentOutOfRangeException("index");
				}

				return objectList[idx];
			}
			set
			{
				if (idx < 0 || idx >= Count)
				{
					throw new ArgumentOutOfRangeException("index");
				}

				objectList[idx] = value;
				objectTable[value.Key] = value.Value;
			}
		}

		/// <summary>
		/// Get/Set the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The associated value.</returns>
		public virtual V this[K key]
		{
			get { return objectTable[key]; }
			set
			{
				if (objectTable.ContainsKey(key))
				{
					objectTable[key] = value;
					objectList[IndexOf(key)] = new KeyValuePair<K, V>(key, value);
				}
				else
				{
					Add(key, value);
				}
			}
		}

		/// <summary>
		/// Get an unordered list of keys.
		/// This collection refers back to the keys in the original Dictionary.
		/// </summary>
		public ICollection<K> Keys
		{
			get { return objectTable.Keys; }
		}

		/// <summary>
		/// Get an unordered list of values.
		/// This collection refers back to the values in the original Dictionary.
		/// </summary>
		public ICollection<V> Values
		{
			get { return objectTable.Values; }
		}

		/// <summary>
		/// Get the ordered list of keys.
		/// This is a copy of the keys in the original Dictionary.
		/// </summary>
		public List<K> OrderedKeys
		{
			get
			{
				List<K> retList = new List<K>();

				foreach (KeyValuePair<K, V> kvp in objectList)
				{
					retList.Add(kvp.Key);
				}

				return retList;
			}
		}

		/// <summary>
		/// Get the ordered list of values.
		/// This is a copy of the values in the original Dictionary.
		/// </summary>
		public List<V> OrderedValues
		{
			get
			{
				List<V> retList = new List<V>();

				foreach (KeyValuePair<K, V> kvp in objectList)
				{
					retList.Add(kvp.Value);
				}

				return retList;
			}
		}

		/// <summary>
		/// Returns the key at the specified index.
		/// </summary>
		/// <param name="idx">The index.</param>
		/// <returns>The key at the index.</returns>
		public K GetKey(int idx)
		{
			if (idx < 0 || idx >= Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			return objectList[idx].Key;
		}

		/// <summary>
		/// Returns the value at the specified index.
		/// </summary>
		/// <param name="idx">The index.</param>
		/// <returns>The value at the index.</returns>
		public V GetValue(int idx)
		{
			if (idx < 0 || idx >= Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			return objectList[idx].Value;
		}

		/// <summary>
		/// Get the index of a particular key.
		/// </summary>
		/// <param name="key">The key to find the index of.</param>
		/// <returns>The index of the key, or -1 if not found.</returns>
		public int IndexOf(K key)
		{
			int ret = -1;

			for (int i = 0; i < objectList.Count; i++)
			{
				if (objectList[i].Key.Equals(key))
				{
					ret = i;
					break;
				}
			}

			return ret;
		}

		/// <summary>
		/// Given the key-value pair, find the index.
		/// </summary>
		/// <param name="kvp">The key-value pair.</param>
		/// <returns>The index, or -1 if not found.</returns>
		public int IndexOf(KeyValuePair<K, V> kvp)
		{
			return IndexOf(kvp.Key);
		}

		/// <summary>
		/// Gets the Dictionary class backing the KeyedList.
		/// </summary>
		public Dictionary<K, V> ObjectTable
		{
			get { return objectTable; }
		}

		/// <summary>
		/// Clears all entries in the KeyedList.
		/// </summary>
		public void Clear()
		{
			objectTable.Clear();
			objectList.Clear();
		}

		/// <summary>
		/// Test if the KeyedList contains the key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>True if the key is found.</returns>
		public bool ContainsKey(K key)
		{
			return objectTable.ContainsKey(key);
		}

		/// <summary>
		/// Test if the KeyedList contains the key in the key-value pair.
		/// </summary>
		/// <param name="kvp">The key-value pair.</param>
		/// <returns>True if the key is found.</returns>
		public bool Contains(KeyValuePair<K, V> kvp)
		{
			return objectTable.ContainsKey(kvp.Key);
		}

		/// <summary>
		/// Adds a key-value pair to the KeyedList.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The associated value.</param>
		public void Add(K key, V value)
		{
			objectTable.Add(key, value);
			objectList.Add(new KeyValuePair<K, V>(key, value));
		}

		/// <summary>
		/// Adds a key-value pair to the KeyedList.
		/// </summary>
		/// <param name="kvp">The KeyValuePair instance.</param>
		public void Add(KeyValuePair<K, V> kvp)
		{
			Add(kvp.Key, kvp.Value);
		}

		/// <summary>
		/// Copy the entire key-value pairs to the KeyValuePair array, starting
		/// at the specified index of the target array.  The array is populated 
		/// as an ordered list.
		/// </summary>
		/// <param name="kvpa">The KeyValuePair array.</param>
		/// <param name="idx">The position to start the copy.</param>
		public void CopyTo(KeyValuePair<K, V>[] kvpa, int idx)
		{
			objectList.CopyTo(kvpa, idx);
		}

		/// <summary>
		/// Insert the key-value at the specified index.
		/// </summary>
		/// <param name="idx">The zero-based insert point.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public void Insert(int idx, K key, V value)
		{
			if ((idx < 0) || (idx > Count))
			{
				throw new ArgumentOutOfRangeException("index");
			}

			objectTable.Add(key, value);
			objectList.Insert(idx, new KeyValuePair<K, V>(key, value));
		}

		/// <summary>
		/// Insert the key-value pair at the specified index location.
		/// </summary>
		/// <param name="idx">The key.</param>
		/// <param name="kvp">The value.</param>
		public void Insert(int idx, KeyValuePair<K, V> kvp)
		{
			if ((idx < 0) || (idx > Count))
			{
				throw new ArgumentOutOfRangeException("index");
			}

			objectTable.Add(kvp.Key, kvp.Value);
			objectList.Insert(idx, kvp);
		}

		/// <summary>
		/// Remove the entry.
		/// </summary>
		/// <param name="key">The key identifying the key-value pair.</param>
		/// <returns>True if removed.</returns>
		public bool Remove(K key)
		{
			bool found = objectTable.Remove(key);

			if (found)
			{
				objectList.RemoveAt(IndexOf(key));
			}

			return found;
		}

		/// <summary>
		/// Remove the key in the specified KeyValuePair instance.  The Value
		/// property is ignored.
		/// </summary>
		/// <param name="kvp">The key-value identifying the entry.</param>
		/// <returns>True if removed.</returns>
		public bool Remove(KeyValuePair<K, V> kvp)
		{
			return Remove(kvp.Key);
		}

		/// <summary>
		/// Remove the entry at the specified index.
		/// </summary>
		/// <param name="idx">The index to the entry to be removed.</param>
		public void RemoveAt(int idx)
		{
			if ((idx < 0) || (idx >= Count))
			{
				throw new ArgumentOutOfRangeException("index");
			}

			objectTable.Remove(objectList[idx].Key);
			objectList.RemoveAt(idx);
		}

		/// <summary>
		/// Attempt to get the value, given the key, without throwing an exception if not found.
		/// </summary>
		/// <param name="key">The key indentifying the entry.</param>
		/// <param name="val">The value, if found.</param>
		/// <returns>True if found.</returns>
		public bool TryGetValue(K key, out V val)
		{
			return objectTable.TryGetValue(key, out val);
		}

		/// <summary>
		/// Returns an ordered System.Collections KeyValuePair objects.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return objectList.GetEnumerator();
		}

		/// <summary>
		/// Returns an ordered KeyValuePair enumerator.
		/// </summary>
		IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
		{
			return objectList.GetEnumerator();
		}
	}
}
