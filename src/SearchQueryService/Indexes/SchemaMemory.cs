using SearchQueryService.Indexes.Models.Azure;
using System.Collections.Generic;

namespace SearchQueryService.Indexes
{
    public class SchemaMemory
    {
        public Dictionary<string, Dictionary<string, AzField>> NestedItems = new();

        public void AddNestedItemToIndex(string index, AzField field)
        {
            if (!NestedItems.ContainsKey(index))
            {
                NestedItems.Add(index, new Dictionary<string, AzField>());
            }

            NestedItems[index].Add(field.Name, field);
        }

        public Dictionary<string, AzField> GetNestedItemsInIndex(string index)
        {
            return NestedItems[index];
        }
    }
}
