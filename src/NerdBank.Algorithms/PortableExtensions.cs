using System;

#if PROFILE328

namespace NerdBank.Algorithms
{
    internal static class PortableExtensions
    {
        internal static Type GetTypeInfo(this Type type) => type;
    }
}

#endif
