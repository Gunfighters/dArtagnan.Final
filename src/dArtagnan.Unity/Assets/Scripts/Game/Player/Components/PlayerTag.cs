using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerTag : MonoBehaviour
    {
        private PlayerModel _model;
        public SerializableReactiveProperty<bool> aliveTag;
        public SerializableReactiveProperty<bool> deadTag;
        public SerializableReactiveProperty<bool> inRoundTag;
        public SerializableReactiveProperty<bool> aliveInRoundTag;
        public SerializableReactiveProperty<bool> alwaysTag;
        public SerializableReactiveProperty<bool> hostTag;

        private readonly List<Transform> _aliveTaggedObjects = new();
        private readonly List<Transform> _deadTaggedObjects = new();
        private readonly List<Transform> _inRoundTaggedObjects = new();
        private readonly List<Transform> _aliveInRoundTaggedObjects = new();
        private readonly List<Transform> _alwaysTaggedObjects = new();
        private readonly List<Transform> _hostTaggedObjects = new();

        private void Awake()
        {
            _model = GetComponent<PlayerModel>();
            _model.Alive.Subscribe(alive =>
            {
                aliveTag.Value = alive;
                deadTag.Value = !alive;
            });
            Include(transform, TagHandle.GetExistingTag("Alive"), _aliveTaggedObjects);
            Include(transform, TagHandle.GetExistingTag("Dead"), _deadTaggedObjects);
            Include(transform, TagHandle.GetExistingTag("InRound"), _inRoundTaggedObjects);
            Include(transform, TagHandle.GetExistingTag("AliveInRound"), _aliveInRoundTaggedObjects);
            Include(transform, TagHandle.GetExistingTag("Always"), _alwaysTaggedObjects);
            Include(transform, TagHandle.GetExistingTag("Host"), _hostTaggedObjects);
            aliveTag.Subscribe(yes => _aliveTaggedObjects.ForEach(o => o.gameObject.SetActive(yes)));
            deadTag.Subscribe(yes => _deadTaggedObjects.ForEach(o => o.gameObject.SetActive(yes)));
            inRoundTag.Subscribe(yes => _inRoundTaggedObjects.ForEach(o => o.gameObject.SetActive(yes)));
            aliveInRoundTag.Subscribe(yes => _aliveInRoundTaggedObjects.ForEach(o => o.gameObject.SetActive(yes)));
            alwaysTag.Subscribe(yes => _alwaysTaggedObjects.ForEach(o => o.gameObject.SetActive(yes)));
            hostTag.CombineLatest(inRoundTag, (isHost, inRound) => isHost && !inRound)
                .Subscribe(showHostGUI => _hostTaggedObjects.ForEach(o => o.gameObject.SetActive(showHostGUI)));
        }

        private void Start()
        {
            GameModel.Instance.State
                .Select(s => s == GameState.Round).Subscribe(inRound => inRoundTag.Value = inRound);
            GameModel.Instance.State
                .Select(s => s == GameState.Round)
                .CombineLatest(_model.Alive, (inRound, alive) => inRound && alive)
                .Subscribe(yes => aliveInRoundTag.Value = yes);
            GameModel.Instance.HostPlayer
                .Select(host => host == _model)
                .Subscribe(isThisHost => hostTag.Value = isThisHost);
        }

        private static void Include(Transform t, TagHandle targetTag, List<Transform> objects)
        {
            if (t.CompareTag(targetTag)) objects.Add(t);
            foreach (Transform child in t) Include(child, targetTag, objects);
        }
    }
}