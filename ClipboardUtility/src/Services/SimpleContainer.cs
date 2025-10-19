using System;
using System.Collections.Concurrent;

namespace ClipboardUtility.src.Services
{
    /// <summary>
    /// 非侵襲で軽量なサービスコンテナ（シングルトン登録・取得のみ）。
    /// 将来 DI ライブラリに置き換え可能。
    /// </summary>
    public sealed class SimpleContainer
    {
        private readonly ConcurrentDictionary<Type, object> _instances = new();

        public void RegisterSingleton<TService>(TService instance) where TService : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _instances[typeof(TService)] = instance!;
        }

        public TService Get<TService>() where TService : class
        {
            if (_instances.TryGetValue(typeof(TService), out var inst))
            {
                return (TService)inst!;
            }

            throw new InvalidOperationException($"Service of type {typeof(TService).FullName} is not registered.");
        }

        public bool TryGet<TService>(out TService? instance) where TService : class
        {
            instance = null;
            if (_instances.TryGetValue(typeof(TService), out var obj))
            {
                instance = (TService)obj!;
                return true;
            }
            return false;
        }
    }
}