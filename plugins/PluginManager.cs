using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.IO;

namespace Oxide.Plugins
{
    [Info("PluginManager", "Quake", "2.2.6")]
    [Description("Полностью стилизованный Sci-fi менеджер для управления плагинами.")]
    public class PluginManager : RustPlugin
    {
        private readonly HashSet<string> _blacklistedPlugins = new HashSet<string> { "UnityCore", "RustCore" };
        private Dictionary<BasePlayer, int> playerPageIndex = new Dictionary<BasePlayer, int>();
        private Dictionary<BasePlayer, string> playerMenus = new Dictionary<BasePlayer, string>();
        private string mainPanel = "PluginManagerUI";
        private string contentPanel = "PluginManagerContent";
        private string pluginPath = "oxide/plugins/";
        private string activeTab = "Main";
        private const int PluginsPerPage = 12;

        [ChatCommand("plug")]
        void OpenMenu(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage("Только администраторы могут использовать это меню.");
                return;
            }
            CloseUI(player);
            ShowMainMenu(player);
        }

        private void ShowMainMenu(BasePlayer player)
        {
            CloseUI(player);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.02 0.02 0.08 0.95", Material = "assets/icons/glow.mat" },
                RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8" },
                CursorEnabled = true
            }, "Hud", mainPanel);

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.15 0.15", AnchorMax = "0.85 0.85" }
            }, mainPanel, contentPanel);

            AddSideButton(container, "Главная", "0.02 0.88", "0.12 0.96", "plug_main", player, activeTab == "Main");
            AddSideButton(container, "Плагины", "0.02 0.76", "0.12 0.84", "plug_plugins", player, activeTab == "Plugins");
            AddSideButton(container, "Настройки", "0.02 0.64", "0.12 0.72", "plug_settings", player, activeTab == "Settings");

            ShowMainText(container);
            AddCloseButton(container);

            CuiHelper.AddUi(player, container);
        }

        private void ShowMainText(CuiElementContainer container)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "Менеджер плагинов\nДобро пожаловать!", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.2 0.6", AnchorMax = "0.8 0.7" }
            }, contentPanel, "MainText");
        }

        private void SwitchPanel(BasePlayer player, Action<CuiElementContainer> createPanel, string newTab)
        {
            activeTab = newTab;
            CuiHelper.DestroyUi(player, contentPanel);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.15 0.15", AnchorMax = "0.85 0.85" }
            }, mainPanel, contentPanel);

            createPanel(container);

            AddSideButton(container, "Главная", "0.02 0.88", "0.12 0.96", "plug_main", player, activeTab == "Main");
            AddSideButton(container, "Плагины", "0.02 0.76", "0.12 0.84", "plug_plugins", player, activeTab == "Plugins");
            AddSideButton(container, "Настройки", "0.02 0.64", "0.12 0.72", "plug_settings", player, activeTab == "Settings");

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("plug_main")]
        void ShowMain(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null) SwitchPanel(player, ShowMainText, "Main");
        }

        [ConsoleCommand("plug_plugins")]
        void ShowPlugins(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            int page = arg.GetInt(0, 0);
            if (player != null)
            {
                SwitchPanel(player, container => AddPluginList(container, player, page), "Plugins");
            }
        }

        [ConsoleCommand("plug_settings")]
        void ShowSettings(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null)
            {
                SwitchPanel(player, container => { }, "Settings");
            }
        }

        private void AddPluginList(CuiElementContainer container, BasePlayer player, int page = 0)
        {
            float yStart = 0.9f;
            float yOffset = 0.136f;

            if (!playerPageIndex.ContainsKey(player))
            {
                playerPageIndex[player] = 0;
            }

            int pageIndex = playerPageIndex[player];

            HashSet<string> loadedPlugins = new HashSet<string>();
            List<string> plugins = new List<string>();

            foreach (var plugin in Interface.Oxide.RootPluginManager.GetPlugins())
            {
                loadedPlugins.Add(plugin.Name);
                plugins.Add(plugin.Name);
            }

            foreach (string file in Directory.GetFiles(pluginPath, "*.cs"))
            {
                string pluginName = Path.GetFileNameWithoutExtension(file);
                if (!loadedPlugins.Contains(pluginName))
                {
                    plugins.Add(pluginName);
                }
            }

            int startIndex = pageIndex * PluginsPerPage;
            int endIndex = Mathf.Min(startIndex + PluginsPerPage, plugins.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                bool isLoaded = Interface.Oxide.RootPluginManager.GetPlugin(plugins[i])?.IsLoaded ?? false;
                AddPluginEntry(container, plugins[i], isLoaded, ref yStart, player);
                yStart -= yOffset;
            }

            AddNavigationButtons(container, pageIndex);
        }

        [ConsoleCommand("pluginmanager.scrolldown")]
        void ScrollDown(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null && playerPageIndex.ContainsKey(player))
            {
                int totalPlugins = GetTotalPluginsCount();
                int maxPage = (int)Math.Ceiling((double)totalPlugins / PluginsPerPage) - 1;

                if (playerPageIndex[player] < maxPage)
                {
                    playerPageIndex[player]++;
                    UpdatePluginList(player);
                }
            }
        }

        [ConsoleCommand("pluginmanager.scrollup")]
        void ScrollUp(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null && playerPageIndex.ContainsKey(player) && playerPageIndex[player] > 0)
            {
                playerPageIndex[player]--;
                UpdatePluginList(player);
            }
        }

        private int GetTotalPluginsCount()
        {
            HashSet<string> plugins = new HashSet<string>();
            foreach (var plugin in Interface.Oxide.RootPluginManager.GetPlugins())
            {
                plugins.Add(plugin.Name);
            }
            foreach (string file in Directory.GetFiles(pluginPath, "*.cs"))
            {
                string pluginName = Path.GetFileNameWithoutExtension(file);
                if (!plugins.Contains(pluginName) && !_blacklistedPlugins.Contains(pluginName))
                {
                    plugins.Add(pluginName);
                }
            }
            return plugins.Count;
        }

        private void UpdatePluginList(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, contentPanel);

            CuiElementContainer container = new CuiElementContainer();
            AddPluginList(container, player, playerPageIndex[player]);

            CuiHelper.AddUi(player, container);
        }

        private void AddNavigationButtons(CuiElementContainer container, int pageIndex)
        {
            container.Add(new CuiButton
            {
                Button = { Command = $"pluginmanager.scrollup {pageIndex}", Color = "0.7 0.7 0.7 0.2" },
                RectTransform = { AnchorMin = "1.135 -0.17", AnchorMax = "1.185 -0.12" },
                Text = { Text = "▲", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, contentPanel);

            container.Add(new CuiButton
            {
                Button = { Command = $"pluginmanager.scrolldown {pageIndex}", Color = "0.7 0.7 0.7 0.2" },
                RectTransform = { AnchorMin = "1.07 -0.17", AnchorMax = "1.12 -0.12" },
                Text = { Text = "▼", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, contentPanel);
        }

        private void AddPluginEntry(CuiElementContainer container, string pluginName, bool isEnabled, ref float yStart, BasePlayer player)
        {
            if (_blacklistedPlugins.Contains(pluginName)) return;

            string switchID = CuiHelper.GetGuid();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.2 0.3 0.8", Material = "assets/icons/glow.mat" },
                RectTransform = { AnchorMin = $"0.05 {yStart + 0.4255}", AnchorMax = $"1.05 {yStart + 0.5255}" },
                CursorEnabled = true
            }, contentPanel, switchID);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.1", AnchorMax = "0.05 0.9" },
                Text = { Text = "•", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, switchID);

            container.Add(new CuiLabel
            {
                Text = { Text = pluginName, FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.06 0.02", AnchorMax = "0.95 0.98" }
            }, switchID);

            container.Add(new CuiButton
            {
                Button = { Command = isEnabled ? $"pluginmanager.unloadplugin {pluginName} {player.UserIDString} {yStart}" : $"pluginmanager.loadplugin {pluginName} {player.UserIDString} {yStart}", Color = isEnabled ? "0 0.8 0 0.8" : "0.8 0 0 0.8" },
                RectTransform = { AnchorMin = "0.8 0.1", AnchorMax = "0.95 0.9" },
                Text = { Text = isEnabled ? "On" : "Off", FontSize = 12, Align = TextAnchor.MiddleCenter }
            }, switchID);
        }

        [ConsoleCommand("pluginmanager.loadplugin")]
        void LoadPlugin(ConsoleSystem.Arg arg)
        {
            string pluginName = arg.GetString(0);
            string playerID = arg.GetString(1);
            float yStart = arg.GetFloat(2);
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(playerID));
            if (player != null)
            {
                Interface.Oxide.ReloadPlugin(pluginName);
                UpdatePluginEntry(player, pluginName, true, yStart);
            }
        }

        [ConsoleCommand("pluginmanager.unloadplugin")]
        void UnloadPlugin(ConsoleSystem.Arg arg)
        {
            string pluginName = arg.GetString(0);
            string playerID = arg.GetString(1);
            float yStart = arg.GetFloat(2);
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(playerID));
            if (player != null)
            {
                Interface.Oxide.UnloadPlugin(pluginName);
                UpdatePluginEntry(player, pluginName, false, yStart);
            }
        }

        private void UpdatePluginEntry(BasePlayer player, string pluginName, bool isEnabled, float yStart)
        {
            string switchID = CuiHelper.GetGuid();
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.2 0.3 0.8", Material = "assets/icons/glow.mat" },
                RectTransform = { AnchorMin = $"0.05 {yStart + 0.4255}", AnchorMax = $"1.05 {yStart + 0.5255}" },
                CursorEnabled = true
            }, contentPanel, switchID);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.1", AnchorMax = "0.05 0.9" },
                Text = { Text = "•", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, switchID);

            container.Add(new CuiLabel
            {
                Text = { Text = pluginName, FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.06 0.02", AnchorMax = "0.95 0.98" }
            }, switchID);

            container.Add(new CuiButton
            {
                Button = { Command = isEnabled ? $"pluginmanager.unloadplugin {pluginName} {player.UserIDString} {yStart}" : $"pluginmanager.loadplugin {pluginName} {player.UserIDString} {yStart}", Color = isEnabled ? "0 0.8 0 0.8" : "0.8 0 0 0.8" },
                RectTransform = { AnchorMin = "0.8 0.1", AnchorMax = "0.95 0.9" },
                Text = { Text = isEnabled ? "On" : "Off", FontSize = 12, Align = TextAnchor.MiddleCenter }
            }, switchID);

            CuiHelper.AddUi(player, container);
        }

        private void AddSideButton(CuiElementContainer container, string text, string anchorMin, string anchorMax, string command, BasePlayer player, bool isActive)
        {
            container.Add(new CuiButton
            {
                Button = { Command = $"global.{command} {player.UserIDString}", Color = isActive ? "0.5 0 0.5 1" : "0.1 0.2 0.4 1", Material = "assets/icons/glow.mat" },
                RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
                Text = { Text = text, FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
            }, mainPanel);
        }

        private void AddCloseButton(CuiElementContainer container)
        {
            container.Add(new CuiButton
            {
                Button = { Close = mainPanel, Color = "1 0 0 0.8" },
                RectTransform = { AnchorMin = "0.95 0.95", AnchorMax = "0.98 0.98" },
                Text = { Text = "X", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, mainPanel);
        }

        private void CloseUI(BasePlayer player)
        {
            if (playerMenus.ContainsKey(player))
            {
                CuiHelper.DestroyUi(player, mainPanel);
                playerMenus.Remove(player);
            }
        }
    }
}