using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

public static class TypeMeta<T>
{
    public static readonly bool IsValueType = typeof(T).IsValueType;

    public static readonly bool IsReferenceOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
    
    public static nuint Size = (nuint)Unsafe.SizeOf<T>();
}