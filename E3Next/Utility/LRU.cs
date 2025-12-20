using MonoCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Utility
{
	public enum LoadingMode { Eager, Lazy, LazyExpanding };

	public enum AccessMode { FIFO, LIFO, Circular };
	public enum CollectionMode
	{
		/// <summary>
		/// This will throw an exception if an attempt to pull more pool object then the pool contains
		/// </summary>
		Exception,

		/// <summary>
		/// <para>This will allow the pool size grow but will create More memory usage</para>
		/// <para>is passing Initialized size will cap out at 250000000 and throw an exception</para>
		/// <para>Don't Use Unless needed use Exception</para>
		/// </summary>
		ExpandException
	}
	public class LRU<TKey, TValue>
	{
		private Dictionary<TKey, ICacheNode> _values;
		private ICacheNode _firstNode;
		private ICacheNode _lastNode;
		private object _lock = new object();
		private object _trimLock = new object();
		private int _maxSize = 0;
		private Boolean _valueUsesIDispose = false;
		private System.Timers.Timer _cleaningTimer = new System.Timers.Timer();
		private Boolean _cleanerCanGoGood = false;
		private Boolean _isCloneable = false;
		private Pool<ICacheNode> _nodePool;
		public Action<TKey, TValue, TValue> _tryUpdate;

		private int _poolMaxSize;
		public Action<TKey, TValue, TValue> TryUpdateAction = null;

		public LRU(Int32 MaxCacheSize = 250000)
		{
			_values = new Dictionary<TKey, ICacheNode>();
			_poolMaxSize = MaxCacheSize;

			_maxSize = MaxCacheSize;
			_nodePool = new Pool<ICacheNode>(_maxSize + 1, p => new CacheNodePool(p), LoadingMode.Lazy, AccessMode.FIFO, CollectionMode.Exception, true);

			if (typeof(TValue).GetInterface("IDisposable") != null)
			{
				_valueUsesIDispose = true;
			}

			if (typeof(TValue).GetInterface("ICloneable") != null)
			{
				_isCloneable = true;
			}

			_cleaningTimer.Interval = 1000;
			_cleaningTimer.AutoReset = false;
			_cleaningTimer.Elapsed += SelfCleanValues;


		}
		public LRU(IEqualityComparer<TKey> comparer,Int32 MaxCacheSize = 250000)
		{
			_values = new Dictionary<TKey, ICacheNode>(comparer);
			_poolMaxSize = MaxCacheSize;
			_maxSize = MaxCacheSize;
			_nodePool = new Pool<ICacheNode>(_maxSize + 1, p => new CacheNodePool(p), LoadingMode.Lazy, AccessMode.FIFO, CollectionMode.Exception, true);

			if (typeof(TValue).GetInterface("IDisposable") != null)
			{
				_valueUsesIDispose = true;
			}

			if (typeof(TValue).GetInterface("ICloneable") != null)
			{
				_isCloneable = true;
			}

			_cleaningTimer.Interval = 1000;
			_cleaningTimer.AutoReset = false;
			_cleaningTimer.Elapsed += SelfCleanValues;


		}
		public Boolean TryUpdate(TKey key, TValue value)
		{

			lock (_lock)
			{
				//This is a point of performance concern. 
				//Since we are in a global lock and at the mercy of try update
				//this can cause our cache to stall.
				//the try update must be very fast to avoid the concerns.
				if (TryUpdateAction != null)
				{
					if (_values.ContainsKey(key))
					{
						ICacheNode tempNode = _values[key];
						MoveToHead(tempNode);
						TryUpdateAction.Invoke(key, value, tempNode.Value);
						return true;
					}

				}
				else
				{
					throw new Exception("Please set a Try Update Action before using try Update");
				}
			}

			return false;
		}

		public void StartSelfCleaner()
		{
			_cleanerCanGoGood = true;
			_cleaningTimer.Start();
		}
		public void StopSelfCleaner()
		{
			_cleanerCanGoGood = false;
			_cleaningTimer.Stop();

		}
		private void SelfCleanValues(object Sender, System.Timers.ElapsedEventArgs e)
		{

			try
			{
				Trim();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
			if (_cleanerCanGoGood)
			{
				_cleaningTimer.Start();
			}
		}

		public bool TryAdd(TKey key, TValue value)
		{
			lock (_lock)
			{
				if (_values.ContainsKey(key))
				{
					MoveToHead(_values[key]);
					return false;
				}

				ICacheNode node = _nodePool.Acquire();
				node.Key = key;
				node.Value = value;
				node.LastAccessed = DateTime.Now.Ticks;

				_values.Add(key, node);

				node.NextNode = _firstNode;
				if (_firstNode != null)
				{
					_firstNode.PreviousNode = node;
				}
				_firstNode = node;

				if (_lastNode == null)
				{
					_lastNode = node;
				}

				if (_values.Count > _maxSize && _lastNode != null)
				{
					var removingNode = _lastNode;

					if (removingNode.PreviousNode != null)
					{
						removingNode.PreviousNode.NextNode = null;
					}

					_lastNode = removingNode.PreviousNode;

					_values.Remove(removingNode.Key);

					if (_valueUsesIDispose)
					{
						IDisposable tempObject = (IDisposable)removingNode.Value;
						tempObject.Dispose();

					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Non Disposeable object");
					}
					removingNode.PreviousNode = null;
					removingNode.Dispose();

				}
			}

			return true;

		}

		public bool ContainsKey(TKey key)
		{
			bool result;
			lock (_trimLock)
			{
				lock (_lock)
				{
					result = _values.ContainsKey(key);

				}
			}

			return result;
		}

		public bool Remove(TKey key)
		{
			bool success = false;
			ICacheNode node;

			lock (_lock)
			{
				if (_values.TryGetValue(key, out node))
				{
					success = _values.Remove(key);

					if (node.PreviousNode != null)
					{
						node.PreviousNode.NextNode = node.NextNode;
					}

					if (node.NextNode != null)
					{
						node.NextNode.PreviousNode = node.PreviousNode;
					}

					//First node and last node should be taken into account else you will have issues where _firstNode
					//and last node point to the diposed object
					if (node == _firstNode)
					{
						_firstNode = node.NextNode;
					}
					if (node == _lastNode)
					{
						_lastNode = node.PreviousNode;
					}


					if (_valueUsesIDispose)
					{
						IDisposable tempObject = (IDisposable)node.Value;
						tempObject.Dispose();

					}

					node.PreviousNode = null;
					node.NextNode = null;
					node.Dispose();

				}
			}

			return success;
		}

		public TValue this[TKey key]
		{
			get
			{
				TValue ReturnObject = default(TValue);

				ICacheNode node;
				lock (_trimLock)
				{
					lock (_lock)
					{
						if (_values.TryGetValue(key, out node))
						{
							if (node != null)
							{
								ReturnObject = node.Value;

								node.LastAccessed = DateTime.Now.Ticks;
								MoveToHead(node);
							}


						}
					}
				}

				// Technical race condition because this node can be put back into a pool
				if (_isCloneable)
				{
					if (!EqualityComparer<TValue>.Default.Equals(ReturnObject, default(TValue)))
					{
						return (TValue)((ICloneable)ReturnObject).Clone();
					}
				}

				return ReturnObject;
			}
			set
			{
				ICacheNode node;
				lock (_lock)
				{
					if (_values.TryGetValue(key, out node))
					{
						node.Value = value;
						node.LastAccessed = DateTime.Now.Ticks;
						MoveToHead(node);
					}
				}
			}
		}

		public void Clear()
		{
			lock (_lock)
			{

				if (_valueUsesIDispose)
				{

					foreach (KeyValuePair<TKey, ICacheNode> pair in _values)
					{
						IDisposable tempObject = (IDisposable)pair.Value.Value;
						tempObject.Dispose();
						pair.Value.Dispose();
					}


				}

				_values.Clear();


				_firstNode = null;
				_lastNode = null;
			}
		}

		public int Count
		{
			get { return _values.Count; }
		}

		public int MaxSize
		{
			get { return _maxSize; }
			set
			{
				if (value > _poolMaxSize)
				{
					throw new Exception(string.Format("Cant Exceed the inital cache size of {0}", _poolMaxSize));
				}

				_maxSize = value;
			}
		}

		private long _expires = new System.TimeSpan(365, 0, 0, 0, 0).Ticks;

		public TimeSpan Expires
		{
			get { return new TimeSpan(_expires); }
			set { _expires = value.Ticks; }
		}

		public void Trim()
		{
			Trim(int.MaxValue);
		}

		public void Trim(int maxTrim)
		{
			lock (_trimLock)
			{
				if (_lastNode != null && _lastNode.LastAccessed + _expires < DateTime.Now.Ticks)
				{
					lock (_lock)
					{
						var currentNode = _lastNode;
						int trimmedCount = 0;

						while (currentNode != null && trimmedCount < maxTrim)
						{
							_values.Remove(currentNode.Key);
							if (_valueUsesIDispose)
							{
								IDisposable tempObject = (IDisposable)currentNode.Value;
								tempObject.Dispose();

							}
							_lastNode = currentNode.PreviousNode;
							if (_lastNode != null)
							{
								_lastNode.NextNode = null;
							}

							if (_firstNode == currentNode)
							{
								_firstNode = null;
							}

							currentNode.PreviousNode = null;
							currentNode.NextNode = null;

							currentNode.Dispose();

							if (_lastNode != null && _lastNode.LastAccessed + _expires < DateTime.Now.Ticks)
							{
								currentNode = _lastNode;
							}
							else
							{
								currentNode = null;
							}


							trimmedCount++;
						}
					}
				}
			}
		}

		private void MoveToHead(ICacheNode node)
		{
			if (node.NextNode == null && node.PreviousNode == null)
			{
				return;
			}

			if (_firstNode == node)
			{
				return;
			}

			//Remove Node from current Location
			if (node.PreviousNode != null)
			{
				node.PreviousNode.NextNode = node.NextNode;
			}

			if (node.NextNode != null)
			{
				node.NextNode.PreviousNode = node.PreviousNode;
			}

			if (_lastNode == node)
			{
				if (node.NextNode != null)
				{
					_lastNode = node.NextNode;
				}
				else
				{
					_lastNode = node.PreviousNode;
				}
			}

			node.NextNode = null;
			node.PreviousNode = null;


			//Insert Node at head.
			if (_firstNode != null && _firstNode != node)
			{
				node.NextNode = _firstNode;
				_firstNode.PreviousNode = node;
			}
			_firstNode = node;

		}

		public class CacheNode : ICacheNode
		{
			public ICacheNode NextNode { get; set; }
			public ICacheNode PreviousNode { get; set; }

			public TKey Key { get; set; }
			public TValue Value { get; set; }

			public long LastAccessed { get; set; }

			public void Dispose()
			{

			}
		}

		public class CacheNodePool : ICacheNode
		{

			private CacheNode InternalObject;

			private Pool<ICacheNode> pool;

			public CacheNodePool(Pool<ICacheNode> pool)
			{
				if (pool == null)
				{
					throw new ArgumentNullException("pool");
				}
				this.pool = pool;
				this.InternalObject = new CacheNode();

			}

			public TKey Key
			{
				get
				{
					return InternalObject.Key;
				}
				set
				{
					InternalObject.Key = value;
				}
			}

			public long LastAccessed
			{
				get
				{
					return InternalObject.LastAccessed;
				}
				set
				{
					InternalObject.LastAccessed = value;
				}
			}

			public ICacheNode NextNode
			{
				get
				{
					return InternalObject.NextNode;
				}
				set
				{
					InternalObject.NextNode = value;
				}
			}

			public ICacheNode PreviousNode
			{
				get
				{
					return InternalObject.PreviousNode;
				}
				set
				{
					InternalObject.PreviousNode = value;
				}
			}

			public TValue Value
			{
				get
				{
					return InternalObject.Value;
				}
				set
				{
					InternalObject.Value = value;
				}
			}

			public void Dispose()
			{
				if (pool.IsDisposed)
				{
				}
				else
				{
					this.Value = default(TValue);
					this.LastAccessed = 0;
					this.NextNode = null;
					this.PreviousNode = null;
					this.Key = default(TKey);
					pool.Release(this);
				}
			}
		}

		public interface ICacheNode : IDisposable
		{
			TKey Key { get; set; }
			long LastAccessed { get; set; }
			ICacheNode NextNode { get; set; }
			ICacheNode PreviousNode { get; set; }
			TValue Value { get; set; }
		}

	}
	public class Pool<T> : IDisposable
	{
		private bool isDisposed;
		private Func<Pool<T>, T> factory;
		private LoadingMode loadingMode;
		private CollectionMode collectionMode;
		private IItemStore itemStore;
		private int size;
		private int count;

		private Int32 sizeLimit = 230000000;

		public int CountOfPool
		{
			get
			{
				lock (itemStore)
				{
					return count;
				}
			}
		}
		/// <summary>
		/// Object Pool
		/// </summary>
		/// <param name="size">Total count of units in the pool Be aware of threads</param>
		/// <param name="factory">The Pool Factory</param>
		public Pool(int size, Func<Pool<T>, T> factory)
			: this(size, factory, LoadingMode.Lazy, AccessMode.FIFO, CollectionMode.Exception, false)
		{
		}

		/// <summary>
		/// Object Pool
		/// </summary>
		/// <param name="size">Total count of units in the pool Be aware of threads</param>
		/// <param name="factory">The Pool Factory</param>
		public Pool(int size, Func<Pool<T>, T> factory,
			LoadingMode loadingMode, AccessMode accessMode, CollectionMode collectionMode = CollectionMode.Exception)
			: this(size, factory, loadingMode, accessMode, collectionMode, false)
		{

		}

		/// <summary>
		/// Object Pool
		/// </summary>
		/// <param name="size">Total count of units in the pool Be aware of threads</param>
		/// <param name="factory">The Pool Factory</param>
		internal Pool(int size, Func<Pool<T>, T> factory, LoadingMode loadingMode, AccessMode accessMode, CollectionMode collectionMode = CollectionMode.Exception, bool ByPassMaxBy1 = false)
		{
			if (size <= 0)
			{
				throw new ArgumentOutOfRangeException("size", size,
					"Argument 'size' must be greater than zero.");
			}
			else if (size > sizeLimit && ByPassMaxBy1 == false)
			{
				throw new Exception(string.Format("MaxSize can't exceed {0} due to  array size limit", sizeLimit));
			}
			else if (size > sizeLimit + 1 && ByPassMaxBy1 == true)
			{
				throw new Exception(string.Format("MaxSize can't exceed {0} due to  array size limit", sizeLimit));
			}
			if (factory == null)
			{
				throw new ArgumentNullException("factory");
			}
			this.size = size;
			this.factory = factory;
			this.loadingMode = loadingMode;
			this.itemStore = CreateItemStore(accessMode, size);
			this.collectionMode = collectionMode;
			if (loadingMode == LoadingMode.Eager)
			{
				PreloadItems();
			}
		}

		public T Acquire()
		{
			lock (itemStore)
			{
				switch (collectionMode)
				{
					case CollectionMode.ExpandException:
						if (itemStore.Count == 0 && count >= sizeLimit)
						{
							throw new Exception("Exhausted the pool object supply");
						}
						break;
					default:
						if (itemStore.Count == 0 && count >= size)
						{
							throw new Exception("Exhausted the pool object supply");
						}
						break;
				}

				switch (loadingMode)
				{
					case LoadingMode.Eager:
						return AcquireEager();
					case LoadingMode.Lazy:
						return AcquireLazy();
					default:
						return AcquireLazyExpanding();
				}
			}
		}


		public void Release(T item)
		{
			lock (itemStore)
			{
				itemStore.Store(item);
			}
		}

		public void Dispose()
		{
			if (isDisposed)
			{
				return;
			}
			isDisposed = true;
			if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
			{
				while (itemStore.Count > 0)
				{
					IDisposable disposable = (IDisposable)itemStore.Fetch();
					disposable.Dispose();
				}
			}
		}

		#region Acquisition

		private T AcquireEager()
		{

			return itemStore.Fetch();

		}

		private T AcquireLazy()
		{

			if (itemStore.Count > 0)
			{
				return itemStore.Fetch();
			}

			count++;
			return factory(this);
		}

		private T AcquireLazyExpanding()
		{
			bool shouldExpand = false;
			if (count < size)
			{
				int newCount = count++;
				if (newCount <= size)
				{
					shouldExpand = true;
				}
				else
				{
					// Another thread took the last spot - use the store instead
					count--;
				}
			}
			if (shouldExpand)
			{
				return factory(this);
			}
			else
			{
				return itemStore.Fetch();
			}
		}

		private void PreloadItems()
		{
			for (int i = 0; i < size; i++)
			{
				T item = factory(this);
				itemStore.Store(item);
			}
			count = size;
		}

		#endregion

		#region Collection Wrappers

		interface IItemStore
		{
			T Fetch();
			void Store(T item);
			int Count { get; }
		}

		private IItemStore CreateItemStore(AccessMode mode, int capacity)
		{
			switch (mode)
			{
				case AccessMode.FIFO:
					return new QueueStore(capacity);
				case AccessMode.LIFO:
					return new StackStore(capacity);
				default:
					Debug.Assert(mode == AccessMode.Circular,
						"Invalid AccessMode in CreateItemStore");
					return new CircularStore(capacity);
			}
		}

		class QueueStore : Queue<T>, IItemStore
		{
			public QueueStore(int capacity)
				: base(capacity)
			{
			}

			public T Fetch()
			{
				return Dequeue();
			}

			public void Store(T item)
			{
				Enqueue(item);
			}
		}

		class StackStore : Stack<T>, IItemStore
		{
			public StackStore(int capacity)
				: base(capacity)
			{
			}

			public T Fetch()
			{
				return Pop();
			}

			public void Store(T item)
			{
				Push(item);
			}
		}

		class CircularStore : IItemStore
		{
			private List<Slot> slots;
			private int freeSlotCount;
			private int position = -1;

			public CircularStore(int capacity)
			{
				slots = new List<Slot>(capacity);
			}

			public T Fetch()
			{
				if (Count == 0)
				{
					throw new InvalidOperationException("The buffer is empty.");
				}
				int startPosition = position;
				do
				{
					Advance();
					Slot slot = slots[position];
					if (!slot.IsInUse)
					{
						slot.IsInUse = true;
						--freeSlotCount;
						return slot.Item;
					}
				} while (startPosition != position);
				throw new InvalidOperationException("No free slots.");
			}

			public void Store(T item)
			{
				Slot slot = slots.Find(s => object.Equals(s.Item, item));
				if (slot == null)
				{
					slot = new Slot(item);
					slots.Add(slot);
				}
				slot.IsInUse = false;
				++freeSlotCount;
			}

			public int Count
			{
				get { return freeSlotCount; }
			}

			private void Advance()
			{
				position = (position + 1) % slots.Count;
			}

			class Slot
			{
				public Slot(T item)
				{
					this.Item = item;
				}

				public T Item { get; private set; }
				public bool IsInUse { get; set; }
			}
		}

		#endregion

		public bool IsDisposed
		{
			get { return isDisposed; }
		}
	}
}
