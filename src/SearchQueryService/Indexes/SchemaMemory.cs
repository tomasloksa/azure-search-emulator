using System.Collections.Generic;

namespace SearchQueryService.Indexes
{
    public class SchemaMemory
    {
        public Dictionary<string, Dictionary<string, List<string>>> NestedItems = new();

        public void AddNestedItemToIndex(string index, Dictionary<string, List<string>> dict)
        {
            NestedItems[index] = dict;
        }

        public Dictionary<string, List<string>> GetNestedItemsInIndex(string index)
        {
            return NestedItems[index];
        }
    }
}
