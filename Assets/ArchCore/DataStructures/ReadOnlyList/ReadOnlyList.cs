namespace System.Collections.Generic
{
    [Serializable]
    public readonly struct ReadOnlyList<T>
    {
        private static readonly List<T> e = new List<T>();
        private readonly List<T> innerList;

        private List<T> List => innerList ?? e;

        public List<T>.Enumerator GetEnumerator() => List.GetEnumerator();
        
        public ReadOnlyList(List<T> innerList)
        {
            this.innerList = innerList;
        }
        
        public int Count => List.Count;

        public T this[int index] => List[index];

        public IEnumerable<T> AsEnumerable() => List;

        public T Find(Predicate<T> match)
        {
            return List.Find(match);
        }
        
        public bool Contains(T item)
        {
            return List.Contains(item);
        }
        
        public static implicit operator ReadOnlyList<T>(List<T> list) => new ReadOnlyList<T>(list);
    } 
}