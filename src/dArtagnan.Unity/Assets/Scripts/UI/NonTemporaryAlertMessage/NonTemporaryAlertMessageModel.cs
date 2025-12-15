using Game;
using R3;
using UnityEngine;

namespace UI.NonTemporaryAlertMessage
{
    public class NonTemporaryAlertMessageModel : MonoBehaviour
    {
        public ReadOnlyReactiveProperty<string> NonTemporaryAlertMessage => GameModel.Instance.NonTemporaryMessage;
    }
}