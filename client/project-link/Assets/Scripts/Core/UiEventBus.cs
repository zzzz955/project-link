using System;
using System.Collections.Generic;

namespace ProjectLink.Core
{
    public static class UiEventBus
    {
        static readonly Dictionary<Type, Delegate> Handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            var type = typeof(T);
            Handlers[type] = Handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var existing)) return;

            var next = Delegate.Remove(existing, handler);
            if (next == null) Handlers.Remove(type);
            else Handlers[type] = next;
        }

        public static void Publish<T>(T payload)
        {
            if (Handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(payload);
        }
    }

    public readonly struct UiBusyChanged
    {
        public UiBusyChanged(string scope, bool isBusy)
        {
            Scope = scope ?? "";
            IsBusy = isBusy;
        }

        public string Scope { get; }
        public bool IsBusy { get; }
    }

    public readonly struct UiErrorRaised
    {
        public UiErrorRaised(string scope, string errorCode, string message, bool blocking = false)
        {
            Scope = scope ?? "";
            ErrorCode = errorCode ?? "";
            Message = message ?? "";
            Blocking = blocking;
        }

        public string Scope { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool Blocking { get; }
    }

    public readonly struct UiViewModelChanged
    {
        public UiViewModelChanged(string scope, object viewModel)
        {
            Scope = scope ?? "";
            ViewModel = viewModel;
        }

        public string Scope { get; }
        public object ViewModel { get; }
    }

    public readonly struct AuthStateChanged
    {
        public AuthStateChanged(bool isAuthenticated, string provider)
        {
            IsAuthenticated = isAuthenticated;
            Provider = provider ?? "";
        }

        public bool IsAuthenticated { get; }
        public string Provider { get; }
    }

    public readonly struct BalanceChanged
    {
        public BalanceChanged(long softBalance) => SoftBalance = softBalance;
        public long SoftBalance { get; }
    }

    public readonly struct InventoryChanged
    {
        public InventoryChanged(int itemId, int quantity) { ItemId = itemId; Quantity = quantity; }
        public int ItemId { get; }
        public int Quantity { get; }
    }
}
