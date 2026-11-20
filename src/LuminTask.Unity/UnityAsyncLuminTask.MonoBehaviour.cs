using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace LuminThread.Unity
{
    public static class MonoBehaviourLuminTask
    {
        private static readonly ConditionalWeakTable<GameObject, CancellationTokenSource> cancellationTokenSources = 
            new ConditionalWeakTable<GameObject, CancellationTokenSource>();

        public static CancellationToken GetOnDestroyCancellationToken(this MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null)
                throw new ArgumentNullException(nameof(monoBehaviour));

            var gameObject = monoBehaviour.gameObject;
            
            if (!cancellationTokenSources.TryGetValue(gameObject, out var cts))
            {
                cts = new CancellationTokenSource();
                cancellationTokenSources.Add(gameObject, cts);
                
                // 当GameObject被销毁时取消令牌
                gameObject.AddComponent<CancellationTokenOnDestroy>().Initialize(cts);
            }
            
            return cts.Token;
        }

        private class CancellationTokenOnDestroy : MonoBehaviour
        {
            private CancellationTokenSource cts;

            public void Initialize(CancellationTokenSource cts)
            {
                this.cts = cts;
            }

            private void OnDestroy()
            {
                try { cts?.Cancel(); }
                finally
                {
                    cancellationTokenSources.Remove(gameObject);
                    cts?.Dispose();
                }
            }
        }
    }

    public static partial class UnityAsyncLuminTask
    {
        public static LuminTask StartAsyncCoroutine(this MonoBehaviour monoBehaviour, Func<CancellationToken, LuminTask> asyncCoroutine)
        {
            var token = monoBehaviour.GetOnDestroyCancellationToken();
            return asyncCoroutine(token);
        }
    }
}