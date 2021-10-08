using System;

namespace SearchQueryService.Exceptions
{
    public class SchemaNotCreatedException : Exception
    {
        public SchemaNotCreatedException() : base()
        {
        }

        public SchemaNotCreatedException(string message) : base(message)
        {
        }

        public SchemaNotCreatedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
