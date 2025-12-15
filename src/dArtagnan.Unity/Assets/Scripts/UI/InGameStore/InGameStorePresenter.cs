using System.Linq;
using Audio;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using R3.Triggers;
using UnityEngine;

namespace UI.InGameStore
{
    public static class InGameStorePresenter
    {
        public static void Initialize(InGameStoreModel model, InGameStoreView view)
        {
            GameModel.Instance.GiftBoxResult.Subscribe(won =>
            {
                if (won)
                    view.giftBoxParticles.Play();
                else
                {
                    view.giftBoxFailureText.enabled = true;
                    UniTask.WaitForSeconds(1).ContinueWith(() => view.giftBoxFailureText.enabled = false);
                }
            });
            GameModel.Instance.LocalPlayer.CurrentValue.Accuracy.Subscribe(acc => view.currentAccuracyText.text = $"현재 명중률: {acc}%");
            model.MaxShopTime.Subscribe(maxTime => view.timeSlider.maxValue = maxTime);
            model.RemainingTime.Subscribe(newTime => view.timeSlider.value = newTime);
            model.RemainingTime
                .Select(r => r / model.MaxShopTime.CurrentValue <= 0.3)
                .Subscribe(hurry =>
                {
                    view.timeSlider.image.sprite = hurry ? view.timeSliderHurryImage : view.timeSliderNormalImage;
                    view.timerImage
                        .UpdateAsObservable()
                        .TakeUntil(model.RemainingTime.Select(r => r <= 0))
                        .Subscribe(_ =>
                            view.timerImage.transform.rotation = Quaternion.Euler(0, 0, VibrateAngle(Time.time)));
                });
            model.AccuracyResetCost.SubscribeToText(view.accuracyResetCostText);
            for (var i = 0; i < model.AccuracyPool.Count; i++)
                view.AccuracySlots[i].Text.text = $"{model.AccuracyPool[i]}%";
            model.AccuracyPool
                .ObserveAdd()
                .Subscribe(newAcc =>
                {
                    view.AccuracySlots[newAcc.Index].Text.text = $"{newAcc.Value}%";
                });
            model.Rotation
                .Subscribe(newRotation => view.spin.rotation = Quaternion.Euler(0, 0, newRotation))
                .AddTo(view);
            view.spinButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                    PacketChannel.Raise(new ShopRouletteFromClient());
                });
            model.CurrentBalance.Subscribe(newBalance =>
            {
                view.spinButton.interactable = newBalance > model.AccuracyResetCost.CurrentValue;
                UpdatePurchaseButtonState(model, view);
            });
            model.CurrentBalance.Select(b => b > model.AccuracyResetCost.CurrentValue)
                .CombineLatest(model.Spin, (enough, spinning) => enough && !spinning)
                .Subscribe(activateSpinButton =>
                {
                    view.spinButton.interactable = activateSpinButton;
                    view.spinButtonGraphics.ForEach(c =>
                    {
                        var color = c.color;
                        color.a = activateSpinButton ? 1f : 0.5f;
                        c.color = color;
                    });
                });
            model.selectedItemIndex.Pairwise().Subscribe(pair =>
            {
                if (pair.Previous != -1)
                    view.itemSlots[pair.Previous].selected.Value = false;
                if (pair.Current == -1) return;
                var selectedItem = model.Items[pair.Current];
                ShowDetail(selectedItem, view);
                UpdatePurchaseButtonState(model, view);
            });
            model.selectedItemIndex.Select(i => i != -1).Subscribe(selected => view.costIcon.enabled = selected);
            for (var i = 0; i < model.Items.Count; i++)
                OnItemAdded(model, view, i, model.Items[i]);
            model.Items.ObserveAdd().Subscribe(added =>
            {
                OnItemAdded(model, view, added.Index, added.Value);
            });
            model.CurrentBalance.Subscribe(newBalance => view.currentBalance.text = newBalance.ToString("N0"));
            model.OwnedItems.Subscribe(arr =>
            {
                view.OwnedItems.ForEach(item => item.gameObject.SetActive(false));
                foreach (var id in arr) OnOwnedItemAdded(view, id);
            });
            view.purchaseButton.Button
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioClipPlayer.Instance.Play(AudioClipType.Purchased);
                    PacketChannel.Raise(new PurchaseItemFromClient
                    {
                        ItemId = model.Items[model.selectedItemIndex.CurrentValue].ItemId
                    });
                });

            // 초기 첫 번째 아이템 선택
            if (model.Items.Count > 0)
            {
                model.selectedItemIndex.Value = 0;
                view.itemSlots[0].selected.Value = true;
            }
        }

        private static void UpdatePurchaseButtonState(InGameStoreModel model, InGameStoreView view)
        {
            if (model.selectedItemIndex.CurrentValue == -1)
            {
                view.purchaseButton.Button.interactable = false;
                return;
            }
            var selectedItem = model.Items[model.selectedItemIndex.CurrentValue];
            view.purchaseButton.Button.interactable = model.CurrentBalance.CurrentValue > selectedItem.Price;
        }

        private static void ShowDetail(ShopItem item, InGameStoreView view)
        {
            view.itemName.text = item.Name;
            view.description.text = item.Description;
            view.cost.text = item.Price.ToString();
        }

        private static void OnItemAdded(InGameStoreModel model, InGameStoreView view, int index, ShopItem item)
        {
            var updatedSlot = view.itemSlots[index];
            updatedSlot.IsClicked.Subscribe(_ =>
            {
                updatedSlot.selected.Value = true;
            });
            updatedSlot.itemId.Value = item.ItemId;
            updatedSlot.selected.Subscribe(selected =>
            {
                if (selected)
                    model.selectedItemIndex.Value = index;
            });
            if (index == model.selectedItemIndex.CurrentValue) ShowDetail(item, view);
        }

        private static void OnOwnedItemAdded(InGameStoreView view, ItemId itemId)
        {
            InGameItemView editedItem;
            if (view.OwnedItems.Any(item => !item.gameObject.activeSelf))
            {
                editedItem = view.OwnedItems.First(item => !item.gameObject.activeSelf);
            }
            else
            {
                editedItem = Object.Instantiate(view.ownedItemPrefab, view.OwnedItemListContainer);
                view.OwnedItems.Add(editedItem);
            }

            var itemMatch = editedItem.shopItemCollection.FindItemByID(itemId);
            editedItem.image.sprite = itemMatch.icon;
            editedItem.showTooltip.Subscribe(show =>
            {
                view.itemTooltip.itemNameText.text = itemMatch.Item.Name;
                view.itemTooltip.itemDescriptionText.text = itemMatch.Item.Description;
                view.itemTooltip.gameObject.SetActive(show);
            });
            editedItem.gameObject.SetActive(true);
        }

        private static float VibrateAngle(float t) => t % 1 < 0.5f ? 0 : Mathf.Sin(t * Mathf.PI * 16) * 15f;
    }
}