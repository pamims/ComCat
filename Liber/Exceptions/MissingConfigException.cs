using System;

namespace ComCat.Exceptions
{
    public class MissingConfigException : Exception
    {
        public MissingConfigException(string keyValue)
            : base($"{keyValue} missing from configuration file.") { }
    }
}
