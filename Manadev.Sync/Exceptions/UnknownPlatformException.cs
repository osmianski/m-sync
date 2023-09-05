using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Manadev.Sync.Exceptions
{
    class UnknownPlatformException : Exception
    {
        public UnknownPlatformException()
            : base()
        {
        }
        public UnknownPlatformException(string message)
            : base(message)
        {
        }

        protected UnknownPlatformException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        public UnknownPlatformException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
