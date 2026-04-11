using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnclaimedAssets.Bootstrap
{
    public class BootstrapController : MonoBehaviour
    {
        private const string GameSceneName = "Game";

        private IEnumerator Start()
        {
            yield return LoadGameData();
            yield return LoadSaveData();
            yield return PreloadSpriteSheet();
            yield return InitYandexSDK();

            Debug.Log("[Bootstrap] Done. Loading Game scene...");
            SceneManager.LoadScene(GameSceneName);
        }

        private IEnumerator LoadGameData()
        {
            Debug.Log("[Bootstrap] Loading GameData...");
            yield return null;
        }

        private IEnumerator LoadSaveData()
        {
            Debug.Log("[Bootstrap] Loading SaveData...");
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
