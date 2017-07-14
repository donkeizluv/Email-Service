using System.Collections.Generic;

namespace AnEmailService.EmailComposer
{
    public class ContentMap
    {
        public ContentMap()
        {
            Map = new List<int>();
        }

        public string Group { get; set; }
        public List<int> Map { get; set; }
    }
}