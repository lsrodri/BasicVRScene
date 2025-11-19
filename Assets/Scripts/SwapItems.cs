using UnityEngine;

public class SwapItems : MonoBehaviour
{
    [SerializeField] private GameObject[] gameObjects;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateObject(0);
    }

    /// <summary>
    /// Activates the GameObject at the specified index and deactivates all others
    /// </summary>
    /// <param name="index">Index of the GameObject to activate</param>
    public void ActivateObject(int index)
    {
        if (gameObjects == null || gameObjects.Length == 0)
        {
            Debug.LogWarning("GameObject array is empty or null.");
            return;
        }

        if (index < 0 || index >= gameObjects.Length)
        {
            Debug.LogWarning($"Index {index} is out of range. Array length: {gameObjects.Length}");
            return;
        }

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
            {
                gameObjects[i].SetActive(i == index);
            }
        }
    }
}
