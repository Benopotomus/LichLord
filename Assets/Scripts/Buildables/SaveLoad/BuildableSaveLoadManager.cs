using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace LichLord.Buildables
{
    public class BuildableSaveLoadManager : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "BuildSaveData.json";
        private string saveFilePath;

        private void Awake()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        }
    }
}