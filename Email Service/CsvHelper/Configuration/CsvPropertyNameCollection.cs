using System.Collections;
using System.Collections.Generic;

namespace CsvHelper.Configuration
{
    /// <summary>
    ///     A collection that holds property names.
    /// </summary>
    public class CsvPropertyNameCollection : IEnumerable<string>, IEnumerable
    {
        /// <summary>
        ///     Gets the count.
        /// </summary>
        public int Count
        {
            get { return Names.Count; }
        }

        /// <summary>
        ///     Gets the name at the given index. If a prefix is set,
        ///     it will be prepended to the name.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string this[int index]
        {
            get { return string.Concat(Prefix, Names[index]); }
            set { Names[index] = value; }
        }

        /// <summary>
        ///     Gets the raw list of names without
        ///     the prefix being prepended.
        /// </summary>
        public List<string> Names { get; } = new List<string>();

        /// <summary>
        ///     Gets the prefix to use for each name.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Names.Count; i++)
                yield return this[i];
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Names.GetEnumerator();
        }

        /// <summary>
        ///     Adds the given name to the collection.
        /// </summary>
        /// <param name="name">The name to add.</param>
        public void Add(string name)
        {
            Names.Add(name);
        }

        /// <summary>
        ///     Adds a range of names to the collection.
        /// </summary>
        /// <param name="names">The range to add.</param>
        public void AddRange(IEnumerable<string> names)
        {
            Names.AddRange(names);
        }

        /// <summary>
        ///     Clears all names from the collection.
        /// </summary>
        public void Clear()
        {
            Names.Clear();
        }
    }
}