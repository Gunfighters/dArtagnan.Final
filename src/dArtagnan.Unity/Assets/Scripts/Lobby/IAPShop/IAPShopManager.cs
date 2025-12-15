using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Lobby.IAPShop
{
    public class IAPShopManager: MonoBehaviour
    {
        public SerializableReactiveProperty<bool> purchaseInProgress;
        private StoreController _storeController;

        private void Awake()
        {
            purchaseInProgress.Subscribe(yes => Debug.Log($"Purchase In Progress: {yes}"));
            Initialize()
                .ContinueWith(SubscribeIAPEvents);
        }

        private void OnDestroy()
        {
            UnsubscribeIAPEvents();
        }

        public async UniTask Initialize()
        {
            try
            {
                _storeController = UnityIAPServices.StoreController();
                await _storeController.Connect().AsUniTask();
                var products = BuildAndFetchProductsWithCatalog();
                _storeController.FetchProducts(products);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private List<ProductDefinition> BuildAndFetchProductsWithCatalog()
        {
            return ProductCatalog
                .LoadDefaultCatalog()
                .allProducts
                .Select(item => new ProductDefinition(item.id, item.type))
                .ToList();
        }

        private void SubscribeIAPEvents()
        {
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
        }

        private void UnsubscribeIAPEvents()
        {
            _storeController.OnProductsFetched -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            _storeController.OnPurchasesFetched -= OnPurchasesFetched;
            _storeController.OnPurchasePending -= OnPurchasePending;
            _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _storeController.OnStoreDisconnected -= OnStoreDisconnected;
            _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed -= OnPurchaseFailed;
        }

        public void PurchaseProduct(string productId)
        {
            if (purchaseInProgress.CurrentValue)
            {
                Debug.LogWarning($"Purchase already in progress");
                return;
            }

            purchaseInProgress.Value = true;
            _storeController.PurchaseProduct(productId);
        }

        private void OnPurchasePending(PendingOrder pending)
        {
            Debug.Log($"Full Receipt JSON: {pending.Info.Receipt}");
            var firstItem = pending.CartOrdered.Items().FirstOrDefault();
            var pid = firstItem?.Product.definition.id;
            if (string.IsNullOrEmpty(pid))
            {
                Debug.LogError($"[IAP] Pending order has no product id");
                return;
            }
            var product = _storeController.GetProductById(pid);
            if (product is null)
            {
                Debug.LogError($"[IAP] Product not found in controller: {pid}");
                return;
            }
            Debug.Log($"[IAP] Purchasing: {pid}");
            Debug.Log($"[IAP] Pending purchase: {product.definition.id}");
            var receipt = pending.Info.Receipt;
            var transactionId = pending.Info.TransactionID;
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(receipt);
            Debug.Log($"[IAP] Purchase receipt: {unifiedReceipt}");
            Debug.Log($"[IAP] Transaction ID: {transactionId}");
            switch (unifiedReceipt.Store)
            {
                case "GooglePlay":
                    Debug.Log($"[IAP] Google IAP: {unifiedReceipt.Payload}");
                    WebsocketManager.Instance.PurchaseIAP(pid, unifiedReceipt.Store, unifiedReceipt.Payload,
                        unifiedReceipt.TransactionID).Forget();
                    break;
                case "AppleAppStore":
                    Debug.Log($"[IAP] Apple IAP: {pending.Info.Apple!.jwsRepresentation}");
                    WebsocketManager.Instance.PurchaseIAP(pid, unifiedReceipt.Store, pending.Info.Apple!.jwsRepresentation,
                        unifiedReceipt.TransactionID).Forget();
                    break;
                default:
                    WebsocketManager.Instance.PurchaseIAP(pid, unifiedReceipt.Store, unifiedReceipt.Payload,
                        unifiedReceipt.TransactionID).Forget();
                    break;
            }
            _storeController.ConfirmPurchase(pending);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            purchaseInProgress.Value = false;
            var purchased = order.CartOrdered.Items().FirstOrDefault()?.Product;
            Debug.Log($"[IAP] Purchase confirmed: {purchased?.definition.id} | Tx: {order.Info.TransactionID}");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            purchaseInProgress.Value = false;
            if (failed.FailureReason == PurchaseFailureReason.UserCancelled)
            {
                Debug.Log($"[IAP] Purchase cancelled by user: {failed.Details}");
            }
            else
            {
                Debug.LogError($"[IAP] Purchase failed: {failed.FailureReason} - {failed.Details}");
            }
        }


        private void OnProductsFetched(List<Product> products)
        {
            _storeController.FetchPurchases();
            Debug.Log($"[IAP] Purchases fetched.");
            foreach (var p in products)
            {
                Debug.Log($"[IAP] {p.definition.id} | {p.metadata.localizedTitle} | {p.metadata.localizedPriceString}");
            }
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError($"[IAP] Product fetch failed: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            // Process purchases, e.g. check for entitlements from completed orders  
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogError($"[IAP] Purchases fetch failed: {failure.FailureReason}");
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription desc)
        {
            purchaseInProgress.Value = false;
            Debug.LogError($"[IAP] Store disconnected: {desc.Message}");
        }
    }
}