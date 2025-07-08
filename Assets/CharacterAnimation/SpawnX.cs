using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnX : MonoBehaviour
{
    [SerializeField] private GameObject GOToDuplicate;
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 5;
    [SerializeField] private float distance = 2f;

    private void Start()
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                GameObject go = Instantiate(GOToDuplicate);
                go.transform.position = new Vector3(j * distance, 0, i * distance);
            }
        }
        GOToDuplicate.SetActive(false);
    }
}
