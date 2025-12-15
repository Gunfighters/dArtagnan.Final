using Game.Player.Components;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class PlayerPoolManager : MonoBehaviour
    {
        public static PlayerPoolManager Instance { get; private set; }
        public PlayerPoolConfig config;
        public IObjectPool<PlayerModel> Pool;

        private void Awake()
        {
            Instance = this;
            Pool = new ObjectPool<PlayerModel>(
                CreateGameObjectBase,
                ActionOnGet,
                ActionOnRelease,
                ActionOnDestroy,
                maxSize: config.poolSize
            );
        }

        private PlayerModel CreateGameObjectBase()
        {
            var gameObjectBase = GameObject.Instantiate(config.playerPrefab).GetComponent<PlayerModel>();
            gameObjectBase.transform.SetParent(transform);
            return gameObjectBase;
        }

        private static void ActionOnGet(PlayerModel playerView)
        {
            playerView.gameObject.SetActive(true);
            playerView.transform.localPosition = Vector3.zero;
        }

        private static void ActionOnRelease(PlayerModel playerView)
        {
            playerView.gameObject.SetActive(false);
        }

        private static void ActionOnDestroy(PlayerModel playerView)
        {
            Destroy(playerView.gameObject);
        }
    }
}