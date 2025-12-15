using Networking;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Lobby
{
    public class NotificationPopupManager : MonoBehaviour
    {
        public NotificationPopup popupPrefab;

        private void Awake()
        {
            while (WebsocketManager.Instance.Notifications.TryDequeue(out var msg))
            {
                var popup = Instantiate(popupPrefab, transform);
                popup.title.Value = msg.title;
                popup.message.Value = msg.body;
            }
            WebsocketManager.Instance.Notifications.ObserveAdd().Subscribe(_ =>
            {
                var msg = WebsocketManager.Instance.Notifications.Dequeue();
                var popup = Instantiate(popupPrefab, transform);
                popup.title.Value = msg.title;
                popup.message.Value = msg.body;
            });
        }
    }
}