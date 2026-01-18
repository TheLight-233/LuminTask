#if UNITY_2023_1_OR_NEWER
namespace LuminThread.Unity
{
    public static class UnityAwaitableExtensions
    {
        public static async LuminTask AsLuminTask(this UnityEngine.Awaitable awaitable)
        {
            await awaitable;
        }
        
        public static async LuminTask<T> AsLuminTask<T>(this UnityEngine.Awaitable<T> awaitable)
        {
            return await awaitable;
        }
    }
}
#endif

