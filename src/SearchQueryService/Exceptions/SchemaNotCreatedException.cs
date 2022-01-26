using System;

namespace SearchQueryService.Exceptions
{
    public class SchemaNotCreatedException : Exception
    {
        public SchemaNotCreatedException() : base()
        {
        }

        public SchemaNotCreatedException(string indexName)
            : base($"====== The schema for index `{indexName}` was not created correctly")
        {
        }
    }
}
