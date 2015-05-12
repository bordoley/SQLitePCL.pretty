using System;
using System.Collections.Generic;

namespace SQLitePCL.pretty
{
    internal sealed class OrderedSet<T> : ICollection<T>
    {
        private static IEnumerator<T> Reverse(LinkedList<T> list) 
        {
            var el = list.Last;
            while (el != null) 
            {
                yield return el.Value;
                el = el.Previous;
            }
        }

        private readonly IDictionary<T, LinkedListNode<T>> set = new Dictionary<T, LinkedListNode<T>>();
        private readonly LinkedList<T> list = new LinkedList<T>();

        public void Add(T item)
        {
            if (!set.ContainsKey(item))
            {
                var node = list.AddLast(item);
                set.Add(item, node);
            }
        }

        public void Clear()
        {
            set.Clear();
            list.Clear();
        }

        public bool Contains(T item) =>
            set.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) =>
            list.CopyTo(array, arrayIndex);

        public int Count
        {
            get { return set.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            LinkedListNode<T> value;
            if (set.TryGetValue(item, out value))
            {
                set.Remove(item);
                list.Remove(value);
            }
            return false;
        }

        public IEnumerable<T> Reverse() =>
            new DelegatingEnumerable<T>(() => Reverse(this.list));

        public IEnumerator<T> GetEnumerator() =>
            this.list.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            this.GetEnumerator();
    }
}
