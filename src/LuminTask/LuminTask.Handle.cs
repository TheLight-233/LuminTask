namespace LuminThread;

internal static class LuminTaskHandleBits
{
    internal static bool IsNull(this ulong handle) => handle == 0;
    internal static uint SlotRaw(this ulong handle) => (ushort)handle;
}
