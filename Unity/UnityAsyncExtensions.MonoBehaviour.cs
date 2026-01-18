using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace LuminThread.Unity
{
    public static class MonoBehaviourExtensions
    {
        private static readonly ConditionalWeakTable<GameObject, CancellationTokenSource> cancellationTokenSources = 
            new ConditionalWeakTable<GameObject, CancellationTokenSource>();

        public static CancellationToken GetCancellationTokenOnDestroy(this MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null)
                throw new ArgumentNullException(nameof(monoBehaviour));

            var gameObject = monoBehaviour.gameObject;
            
            if (!cancellationTokenSources.TryGetValue(gameObject, out var cts))
            {
                cts = new CancellationTokenSource();
                cancellationTokenSources.Add(gameObject, cts);
                
                // 当GameObject被销毁时取消令牌
                monoBehaviour.gameObject.AddComponent<CancellationTokenOnDestroy>().Initialize(cts, () =>
                {
                    cancellationTokenSources.Remove(gameObject);
                });
            }
            
            return cts.Token;
        }

        private class CancellationTokenOnDestroy : MonoBehaviour
        {
            private CancellationTokenSource cts;
            private Action onDestroyCallback;

            public void Initialize(CancellationTokenSource cts, Action onDestroyCallback)
            {
                this.cts = cts;
                this.onDestroyCallback = onDestroyCallback;
            }

            private void OnDestroy()
            {
                cts?.Cancel();
                cts?.Dispose();
                onDestroyCallback?.Invoke();
            }
        }
    }

    public static partial class UnityAsyncExtensions
    {
        public static LuminTask StartAsyncCoroutine(this MonoBehaviour monoBehaviour, Func<CancellationToken, LuminTask> asyncCoroutine)
        {
            var token = monoBehaviour.GetCancellationTokenOnDestroy();
            return asyncCoroutine(token);
        }
    }
}