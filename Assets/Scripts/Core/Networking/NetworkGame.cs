#define ENABLE_LOGS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

namespace LichLord
{

    public sealed class NetworkGame : ContextBehaviour, IPlayerJoined, IPlayerLeft
    {
        // PUBLIC MEMBERS
        public List<PlayerCharacter> ActivePlayers = new List<PlayerCharacter>();
        public event Action GameLoaded;

        private PlayerRef _localPlayer;
        private FusionCallbacksHandler _fusionCallbacks = new FusionCallbacksHandler();

        public void Initialize()
        {
            _localPlayer = Runner.LocalPlayer;
            ActivePlayers.Clear();
            _fusionCallbacks.DisconnectedFromServer -= OnDisconnectedFromServer;
            _fusionCallbacks.DisconnectedFromServer += OnDisconnectedFromServer;
            Runner.RemoveCallbacks(_fusionCallbacks);
            Runner.AddCallbacks(_fusionCallbacks);
        }

        public IEnumerator Activate(int levelIndex, int levelGeneratorSeed)
        {
            /*
            while (Context.GameplayMode == null)
                yield return null;

            while (Context.LevelManager == null)
                yield return null;

            Debug.Log("Activating Game Mode");
            Context.GameplayMode.Activate();

            while (Context.LevelManager.LoadedLevel == null)
                yield return null;

            */

            Cursor.lockState = CursorLockMode.Locked;

            GameLoaded?.Invoke();
            yield return null;
        }

        public PlayerCharacter GetPlayerCharacter(PlayerRef playerRef)
        {
            if (!playerRef.IsRealPlayer)
                return null;

            foreach (PlayerCharacter player in ActivePlayers)
            {
                if (player.Object.InputAuthority == playerRef)
                    return player;
            }

            return null;
        }

        public override void FixedUpdateNetwork()
        {

        }

        // IPlayerJoined/IPlayerLeft INTERFACES
        void IPlayerJoined.PlayerJoined(PlayerRef playerRef)
        {
        }

        void IPlayerLeft.PlayerLeft(PlayerRef playerRef)
        {
        }

        // PRIVATE METHODS

        private void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Log.Info($"Disconnected from server: {reason}");
        }

    }
}