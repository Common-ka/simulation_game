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

            Debug.Log("[Bootstrap] Done. Running tests...");
            Debug.Log($"[NumberFormatter Test] 123 -> {UnclaimedAssets.Utils.NumberFormatter.Format(123)}");
            Debug.Log($"[NumberFormatter Test] 1234 -> {UnclaimedAssets.Utils.NumberFormatter.Format(1234)}");
            Debug.Log($"[NumberFormatter Test] 1234567 -> {UnclaimedAssets.Utils.NumberFormatter.Format(1234567)}");
            Debug.Log($"[NumberFormatter Test] 1234567890 -> {UnclaimedAssets.Utils.NumberFormatter.Format(1234567890)}");
            Debug.Log($"[NumberFormatter Test] 1234567890123 -> {UnclaimedAssets.Utils.NumberFormatter.Format(1234567890123)}");
            Debug.Log($"[NumberFormatter Test] 1234567890123456 -> {UnclaimedAssets.Utils.NumberFormatter.Format(1234567890123456)}");

            Debug.Log("[Bootstrap] Loading Game scene...");
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
