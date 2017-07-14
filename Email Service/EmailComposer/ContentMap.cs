using System.Collections.Generic;
using System.Linq;

namespace AnEmailService.EmailComposer
{
    public class ContentMap
    {
        public ContentMap(string group)
        {
            Group = group;
        }
        public void Add(int index, string[] row)
        {
            ContentDict.Add(index, row);
        }
        public List<string[]> GetContentArray()
        {
            return ContentDict.Values.ToList();
        }
        public string Group { get; private set; }
        public Dictionary<int, string[]> ContentDict { get; private set; } = new Dictionary<int, string[]>();
    }
}