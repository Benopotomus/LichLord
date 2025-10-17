using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class DebugConsole : ContextBehaviour
    {
        [SerializeField] private UIDebugConsoleView _consoleView;
        private Dictionary<string, ConsoleCommand> _commands = new Dictionary<string, ConsoleCommand>();

        public override void Spawned()
        {
            base.Spawned();

            if (Context.UI is GameplayUI gameplay)
            {
                _consoleView = gameplay.DebugConsoleView;
            }

            RegisterDefaultCommands();
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ExecuteCommand(string command)
        {
            var parts = command.Split(' ');
            var cmdName = parts[0].ToLower();
            var args = new List<string>(parts);
            args.RemoveAt(0);

            if (_commands.TryGetValue(cmdName, out var cmd))
            {
                try
                {
                    cmd.Action?.Invoke(args.ToArray());
                    _consoleView?.AddLog(EConsoleColor.Success, $"Executed: {command}");
                }
                catch (Exception e)
                {
                    _consoleView?.AddLog(EConsoleColor.Error, $"Error in {cmdName}: {e.Message}");
                }
            }
            else
            {
                _consoleView?.AddLog(EConsoleColor.Warning, $"Unknown command: {cmdName}");
                ShowHelp();
            }
        }

        public void RegisterCommand(string name, string description, Action<string[]> action)
        {
            _commands[name.ToLower()] = new ConsoleCommand { Name = name, Description = description, Action = action };
            _consoleView?.AddLog(EConsoleColor.Info, $"Registered command: {name}");
        }

        private void RegisterDefaultCommands()
        {
            RegisterCommand("help", "Show all commands", _ => ShowHelp());
            RegisterCommand("stat", "Show game stats", _ => ShowStats());
        }

        private void ShowHelp()
        {
            var helpText = "Available commands:\n";
            foreach (var cmd in _commands.Values)
            {
                helpText += $"  {cmd.Name} - {cmd.Description}\n";
            }
            _consoleView?.AddLog(EConsoleColor.Info, helpText);
        }

        private void ShowStats()
        {
            _consoleView?.AddLog(EConsoleColor.Info, $"FPS: {1f / Time.unscaledDeltaTime:F1}");
            _consoleView?.AddLog(EConsoleColor.Info, $"Time: {Time.time:F1}s");
        }
    }

    public class ConsoleCommand
    {
        public string Name;
        public string Description;
        public Action<string[]> Action;
    }
}