using Fusion;
using LichLord.NonPlayerCharacters;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class DebugConsole : ContextBehaviour
    {
        [SerializeField] private UIDebugConsoleView _consoleView;
        private readonly Dictionary<string, ConsoleCommand> _commands = new();

        public override void Spawned()
        {
            base.Spawned();

            if (Context.UI is GameplayUI gameplay)
                _consoleView = gameplay.DebugConsoleView;

            RegisterDefaultCommands();
        }

        // ─────────────────────────────────────────────
        //  Single Entry Point for All Commands
        // ─────────────────────────────────────────────

        public void ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return;

            string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmdName = parts[0].ToLowerInvariant();
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (!_commands.TryGetValue(cmdName, out var cmd))
            {
                Log(EConsoleColor.Warning, $"Unknown command: {cmdName}");
                ShowHelp();
                return;
            }

            try
            {
                cmd.Action?.Invoke(args);
                Log(EConsoleColor.Success, $"Executed: {commandLine}");
            }
            catch (Exception e)
            {
                Log(EConsoleColor.Error, $"Error in {cmdName}: {e.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  Default Commands
        // ─────────────────────────────────────────────

        private void RegisterDefaultCommands()
        {
            RegisterCommand("help", "Show all available commands", _ => ShowHelp());
            RegisterCommand("stat", "Display performance stats", _ => ShowStats());
            RegisterCommand("invasion", "Start an invasion (usage: invasion <level>)", ExecuteInvasion);
            RegisterCommand("spawn.npc", "Spawn an npc at the player's aim position", SpawnNPC);
        }

        private void ExecuteInvasion(string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int definitionId))
            {
                Log(EConsoleColor.Warning, "Usage: invasion <int level>");
                return;
            }

            Log(EConsoleColor.Info, $"Invasion started at level {definitionId}!");

            RPC_BeginInvasion((byte)definitionId, 0);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_BeginInvasion(byte definitionId, byte strongholdId)
        {
            Context.InvasionManager.BeginInvasion(definitionId, strongholdId);
        }

        private void SpawnNPC(string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int definitionId))
            {
                Log(EConsoleColor.Warning, "Usage: spawn.npc <int definitionId>");
                return;
            }

            Vector3 spawnPosition = Context.Camera.CachedRaycastHit.position;

            RPC_SpawnNPC(spawnPosition, definitionId);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SpawnNPC(Vector3 position, int definitionId)
        {
            var definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(definitionId);
            if (definition == null)
            {
                Log(EConsoleColor.Warning, $"Invalid NPC definition ID: {definitionId}");
                return;
            }

            if (HasStateAuthority)
            {
                Context.NonPlayerCharacterManager.SpawnNPCInvader(
                    position,
                    definition,
                    ETeamID.EnemiesTeamA,
                    EAttitude.Hostile,
                    0
                );
            }

            Log(EConsoleColor.Info, $"Spawned NPC {definitionId} at {position}");
        }

        // ─────────────────────────────────────────────
        //  Utilities
        // ─────────────────────────────────────────────

        private void RegisterCommand(string name, string description, Action<string[]> action)
        {
            _commands[name.ToLowerInvariant()] = new ConsoleCommand
            {
                Name = name,
                Description = description,
                Action = action
            };
        }

        private void ShowHelp()
        {
            var helpText = "Available commands:\n";
            foreach (var cmd in _commands.Values)
                helpText += $"  {cmd.Name} - {cmd.Description}\n";
            Log(EConsoleColor.Info, helpText);
        }

        private void ShowStats()
        {
            Log(EConsoleColor.Info, $"FPS: {1f / Time.unscaledDeltaTime:F1}");
            Log(EConsoleColor.Info, $"Time: {Time.time:F1}s");
        }

        private void Log(EConsoleColor color, string message)
        {
            _consoleView?.AddLog(color, message);
        }
    }

    public class ConsoleCommand
    {
        public string Name;
        public string Description;
        public Action<string[]> Action;
    }
}
