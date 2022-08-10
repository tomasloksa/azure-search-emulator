using SearchQueryService.Indexes.Models.Azure;
using System.Collections.Generic;

namespace SearchQueryService.Indexes
{
    public class SchemaMemory
    {
        private readonly Dictionary<string, Dictionary<string, AzField>> _nestedItems = new();

        public void AddNestedItemToIndex(string index, AzField field)
        {
            if (!_nestedItems.ContainsKey(index))
            {
                _nestedItems.Add(index, new Dictionary<string, AzField>());
            }

            _nestedItems[index].Add(field.Name, field);
        }

        public Dictionary<string, AzField> GetNestedItemsInIndex(string index)
        {
            return _nestedItems.GetValueOrDefault(index);
        }
    }
}
