using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust; // Используем правильное пространство
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WelcomeMessage", "Quake", "1.0.0")]
    [Description("Плагин отправляет приветственное сообщение новым игрокам.")]

    public class WelcomeMessage : RustPlugin
    {
        void OnPlayerConnected(BasePlayer player)
        { 
            if (player == null) return;
            player.ChatMessage($"[💬] Добро пожаловать на сервер, {player.displayName}!");
        }
    }
}
