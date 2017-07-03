using System;

namespace NerdBank.Algorithms
{
    internal static class PortableExtensions
    {
#if PROFILE328 || NET40
        internal static Type GetTypeInfo(this Type type) => type;
#endif
    }

#if PROFILE328 || PROFILE259 || NETSTANDARD1_0

    /// <summary>
    /// Stub SerializableAttribute for those profiles that don't expose one.
    /// </summary>
    internal sealed class SerializableAttribute : Attribute
    {
    }

#endif
}
