using System.Collections.Generic;
using UnityEngine;

namespace ProjectLink.Core
{
    public class UserDataCache : MonoBehaviour
    {
        public static UserDataCache Instance { get; private set; }

        long _softBalance;
        readonly Dictionary<int, int> _inventory = new();

        public long SoftBalance => _softBalance;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetBalance(long softBalance)
        {
            _softBalance = softBalance;
            UiEventBus.Publish(new BalanceChanged(softBalance));
        }

        public void SetInventoryItem(int itemId, int quantity)
        {
            _inventory[itemId] = quantity;
            UiEventBus.Publish(new InventoryChanged(itemId, quantity));
        }

        public int GetInventoryItem(int itemId) =>
            _inventory.TryGetValue(itemId, out var qty) ? qty : 0;

        public IReadOnlyDictionary<int, int> GetInventory() => _inventory;

        public void Clear()
        {
            _softBalance = 0;
            _inventory.Clear();
        }
    }
}
