using UnityEngine;

namespace ArchCore.Utils
{
    [CreateAssetMenu(fileName = "RUNTIME-EDITOR_PRELOAD_ASSETS", menuName = "Preload asset container", order = 1)]
    public class AssetContainer : ScriptableObject
    {
        [SerializeField] private Object[] assets;
    }
}