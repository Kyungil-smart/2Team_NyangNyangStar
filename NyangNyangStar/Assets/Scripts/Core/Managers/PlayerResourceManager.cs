using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Managers
{
    public enum PlayerResourceType
    {
        Energy,
        Coin,
        Jewel
    }

    public sealed class PlayerResourceManager
    {
        public static PlayerResourceManager Instance { get; } = new PlayerResourceManager();

        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);
        private ResourcesSO _resourcesSO;

        public event Action ResourcesChanged;

        public int Energy => GetAmount(PlayerResourceType.Energy);
        public int Coin => GetAmount(PlayerResourceType.Coin);
        public int Jewel => GetAmount(PlayerResourceType.Jewel);

        private PlayerResourceManager()
        {
        }

        public void Bind(ResourcesSO resourcesSO)
        {
            if (resourcesSO == null || _resourcesSO == resourcesSO)
                return;

            _resourcesSO = resourcesSO;
            NotifyChanged();
        }

        public int GetAmount(PlayerResourceType type)
        {
            ResourcesSO resourcesSO = ResolveResourcesSO(false);

            if (resourcesSO == null)
                return 0;

            return type switch
            {
                PlayerResourceType.Energy => resourcesSO.energy,
                PlayerResourceType.Coin => resourcesSO.coin,
                PlayerResourceType.Jewel => resourcesSO.jewel,
                _ => 0
            };
        }

        public bool HasEnough(PlayerResourceType type, int amount)
        {
            if (amount <= 0)
                return true;

            return GetAmount(type) >= amount;
        }

        public async Task<bool> RefreshAsync()
        {
            ResourcesSO resourcesSO = ResolveResourcesSO();

            if (resourcesSO == null)
                return false;

            if (!CanUseFirestore())
            {
                NotifyChanged();
                return false;
            }

            await _operationLock.WaitAsync();

            try
            {
                await resourcesSO.UpdateFromServerAsync(false);
                NotifyChanged();
                return true;
            }
            catch (Exception e)
            {
                DebugTool.Warning($"[PlayerResourceManager] Resource refresh failed: {e.Message}", DebugType.Data);
                return false;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task<bool> TrySpendAsync(PlayerResourceType type, int amount, bool refreshFromServer = true)
        {
            if (amount <= 0)
                return true;

            ResourcesSO resourcesSO = ResolveResourcesSO();

            if (resourcesSO == null || !CanUseFirestore())
                return false;

            await _operationLock.WaitAsync();

            int beforeEnergy = resourcesSO.energy;
            int beforeCoin = resourcesSO.coin;
            int beforeJewel = resourcesSO.jewel;
            int beforeUsedEnergy = resourcesSO.usedEnergy;

            try
            {
                if (refreshFromServer)
                    await resourcesSO.UpdateFromServerAsync(false);

                beforeEnergy = resourcesSO.energy;
                beforeCoin = resourcesSO.coin;
                beforeJewel = resourcesSO.jewel;
                beforeUsedEnergy = resourcesSO.usedEnergy;

                if (GetAmount(resourcesSO, type) < amount)
                {
                    NotifyChanged();
                    return false;
                }

                AddAmount(resourcesSO, type, -amount);

                if (type == PlayerResourceType.Energy)
                    resourcesSO.usedEnergy += amount;

                await resourcesSO.UpdateDataAsync();
                NotifyChanged();
                return true;
            }
            catch (Exception e)
            {
                resourcesSO.energy = beforeEnergy;
                resourcesSO.coin = beforeCoin;
                resourcesSO.jewel = beforeJewel;
                resourcesSO.usedEnergy = beforeUsedEnergy;

                DebugTool.Warning($"[PlayerResourceManager] Resource spend failed: {e.Message}", DebugType.Data);
                NotifyChanged();
                return false;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task<bool> AddAsync(
            PlayerResourceType type,
            int amount,
            bool refreshFromServer = true,
            bool trackTotal = true)
        {
            if (amount <= 0)
                return true;

            ResourcesSO resourcesSO = ResolveResourcesSO();

            if (resourcesSO == null || !CanUseFirestore())
                return false;

            await _operationLock.WaitAsync();

            int beforeEnergy = resourcesSO.energy;
            int beforeCoin = resourcesSO.coin;
            int beforeJewel = resourcesSO.jewel;
            int beforeTotalEnergy = resourcesSO.totalGottenEnergy;
            int beforeTotalCoin = resourcesSO.totalGottenCoin;
            int beforeTotalJewel = resourcesSO.totalGottenJewel;

            try
            {
                if (refreshFromServer)
                    await resourcesSO.UpdateFromServerAsync(false);

                beforeEnergy = resourcesSO.energy;
                beforeCoin = resourcesSO.coin;
                beforeJewel = resourcesSO.jewel;
                beforeTotalEnergy = resourcesSO.totalGottenEnergy;
                beforeTotalCoin = resourcesSO.totalGottenCoin;
                beforeTotalJewel = resourcesSO.totalGottenJewel;

                AddAmount(resourcesSO, type, amount);

                if (trackTotal)
                    AddTotalGotten(resourcesSO, type, amount);

                await resourcesSO.UpdateDataAsync();
                NotifyChanged();
                return true;
            }
            catch (Exception e)
            {
                resourcesSO.energy = beforeEnergy;
                resourcesSO.coin = beforeCoin;
                resourcesSO.jewel = beforeJewel;
                resourcesSO.totalGottenEnergy = beforeTotalEnergy;
                resourcesSO.totalGottenCoin = beforeTotalCoin;
                resourcesSO.totalGottenJewel = beforeTotalJewel;

                DebugTool.Warning($"[PlayerResourceManager] Resource add failed: {e.Message}", DebugType.Data);
                NotifyChanged();
                return false;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public Task<bool> TrySpendEnergyAsync(int amount, bool refreshFromServer = true)
        {
            return TrySpendAsync(PlayerResourceType.Energy, amount, refreshFromServer);
        }

        public Task<bool> AddEnergyAsync(int amount, bool refreshFromServer = true, bool trackTotal = true)
        {
            return AddAsync(PlayerResourceType.Energy, amount, refreshFromServer, trackTotal);
        }

        public Task<bool> AddCoinAsync(int amount, bool refreshFromServer = true, bool trackTotal = true)
        {
            return AddAsync(PlayerResourceType.Coin, amount, refreshFromServer, trackTotal);
        }

        public Task<bool> AddJewelAsync(int amount, bool refreshFromServer = true, bool trackTotal = true)
        {
            return AddAsync(PlayerResourceType.Jewel, amount, refreshFromServer, trackTotal);
        }

        private ResourcesSO ResolveResourcesSO(bool warnIfMissing = true)
        {
            if (_resourcesSO != null)
                return _resourcesSO;

            if (FireStoreManager.Instance != null &&
                FireStoreManager.Instance.TryGetStore(out ResourcesSO resourcesSO))
            {
                _resourcesSO = resourcesSO;
                return _resourcesSO;
            }

            if (warnIfMissing)
                DebugTool.Warning("[PlayerResourceManager] ResourcesSO could not be resolved.", DebugType.Data);

            return null;
        }

        private static bool CanUseFirestore()
        {
            if (FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized)
                return true;

            DebugTool.Warning("[PlayerResourceManager] Firestore is not initialized.", DebugType.Data);
            return false;
        }

        private static int GetAmount(ResourcesSO resourcesSO, PlayerResourceType type)
        {
            return type switch
            {
                PlayerResourceType.Energy => resourcesSO.energy,
                PlayerResourceType.Coin => resourcesSO.coin,
                PlayerResourceType.Jewel => resourcesSO.jewel,
                _ => 0
            };
        }

        private static void AddAmount(ResourcesSO resourcesSO, PlayerResourceType type, int delta)
        {
            switch (type)
            {
                case PlayerResourceType.Energy:
                    resourcesSO.energy = Mathf.Max(0, resourcesSO.energy + delta);
                    break;
                case PlayerResourceType.Coin:
                    resourcesSO.coin = Mathf.Max(0, resourcesSO.coin + delta);
                    break;
                case PlayerResourceType.Jewel:
                    resourcesSO.jewel = Mathf.Max(0, resourcesSO.jewel + delta);
                    break;
            }
        }

        private static void AddTotalGotten(ResourcesSO resourcesSO, PlayerResourceType type, int amount)
        {
            switch (type)
            {
                case PlayerResourceType.Energy:
                    resourcesSO.totalGottenEnergy += amount;
                    break;
                case PlayerResourceType.Coin:
                    resourcesSO.totalGottenCoin += amount;
                    break;
                case PlayerResourceType.Jewel:
                    resourcesSO.totalGottenJewel += amount;
                    break;
            }
        }

        private void NotifyChanged()
        {
            ResourcesChanged?.Invoke();
        }
    }
}
