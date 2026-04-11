using System.Collections;
using UnclaimedAssets.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnclaimedAssets.Bootstrap
{
    [RequireComponent(typeof(GameDataLoader))]
    public class BootstrapController : MonoBehaviour
    {
        private const string GameSceneName = "Game";

        private GameDataLoader _gameDataLoader;

        private void Awake()
        {
            _gameDataLoader = GetComponent<GameDataLoader>();
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(_gameDataLoader.LoadAsync());
            yield return LoadSaveData();
            yield return PreloadSpriteSheet();
            yield return InitYandexSDK();

            Debug.Log("[Bootstrap] Done. Loading Game scene...");
            SceneManager.LoadScene(GameSceneName);
        }

        private IEnumerator LoadSaveData()
        {
            Debug.Log("[Bootstrap] Loading SaveData (stub)...");
            yield return null;
        }

        private IEnumerator PreloadSpriteSheet()
        {
            Debug.Log("[Bootstrap] Preloading SpriteSheet (stub)...");
            yield return null;
        }

        private IEnumerator InitYandexSDK()
        {
            Debug.Log("[Bootstrap] Yandex SDK Init (stub)...");
            yield return null;
        }
    }
}
