using UnityEngine;

[DisallowMultipleComponent]
public class TerrainID : MonoBehaviour
{
    [SerializeField] private string id = System.Guid.NewGuid().ToString();

    public string ID => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
#endif
}
