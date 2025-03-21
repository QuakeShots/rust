using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CultistTrader", "QuakeShot", "2.5")]
    [Description("Культист с GUI-торговлей, как у NPC в рыбацкой деревне")]
    public class CultistTrader : RustPlugin
    {
        private Timer spawnTimer;
        private BasePlayer cultistNPC;
        private Vector3 currentSpawnPoint;
        private string currentLocationName;

        private Dictionary<string, Vector3> spawnLocations = new Dictionary<string, Vector3>
        {
            { "Заправке", new Vector3(-800, 50, 700) },
            { "Супермаркете", new Vector3(500, 50, -200) },
            { "Спутниковой станции", new Vector3(1200, 50, -600) }
        };
         
        private Dictionary<string, int> tradeItems = new Dictionary<string, int>
        {
            { "rifle.ak", 5 }, // АК стоит 5 черепов
            { "smg.mp5", 3 },  // MP5 стоит 3 черепа
            { "hazmatsuit", 2 } // Костюм стоит 2 черепа
        };

        private void Init()
        {
            ScheduleNextSpawn();
        }

        private void ScheduleNextSpawn()
        {
            spawnTimer = timer.Once(10800, SpawnCultist);
        }

        private void SpawnCultist()
        {
            if (cultistNPC != null && !cultistNPC.IsDestroyed)
            {
                cultistNPC.Kill();
                cultistNPC = null;
            }

            Vector3 spawnPoint = FindSafeMonumentSpawn(null, out string monumentName);
            if (spawnPoint == Vector3.zero)
            {
                PrintError("Failed to find a suitable spawn point!");
                return;
            }

            currentSpawnPoint = spawnPoint;
            currentLocationName = monumentName;

            if (string.IsNullOrEmpty(currentLocationName) || currentLocationName == "Unknown location")
            {
                PrintError("Error: Spawn location is undefined! Using fallback value.");
                currentLocationName = "a mysterious monument";
            }

            PrintWarning($"Spawning Cultist near {currentLocationName} (Координаты: {currentSpawnPoint})");

            BasePlayer botPlayer = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", currentSpawnPoint) as BasePlayer;
            if (botPlayer == null)
            {
                PrintError("Error: Failed to create Cultist!");
                return;
            }

            botPlayer.Spawn();
            botPlayer.displayName = "Dark Cultist";
            botPlayer.health = 250f;
            botPlayer.SendNetworkUpdate();

            cultistNPC = botPlayer;

            PrintToChat($"<color=#800080>[Cultist]</color> Загадочная фигура появилась у <color=#FF0000>{currentLocationName}</color>...");
        }

        private Vector3 FindSafeMonumentSpawn(BasePlayer player, out string monumentName)
        {
            List<MonumentInfo> allMonuments = TerrainMeta.Path.Monuments;
            monumentName = "Неизвестное место";

            if (allMonuments == null || allMonuments.Count == 0)
                return Vector3.zero;

            List<MonumentInfo> validMonuments = allMonuments.Where(mon => !IsBlacklisted(mon)).ToList();
            if (validMonuments.Count == 0)
            {
                PrintError("❌ Нет подходящих монументов для спавна!");
                return Vector3.zero;
            }

            foreach (var monument in validMonuments.OrderBy(x => UnityEngine.Random.value))
            {
                string monName = GetMonumentName(monument);

                if (monName.ToLower().Contains("cave") || monName.ToLower().Contains("underground"))
                {
                    PrintWarning($"⚠ Найден Underground Cave ({monName}), ищем другое место...");
                    continue;
                }

                Vector3 keyEntityPos = FindKeyEntityNearMonument(monument);
                if (keyEntityPos != Vector3.zero)
                {
                    monumentName = monName;
                    return keyEntityPos;
                }
            }

            PrintError("❌ Не удалось найти подходящую точку спавна!");
            return Vector3.zero;
        }

        private Vector3 FindKeyEntityNearMonument(MonumentInfo monument)
        {
            Vector3 centerPos = monument.Bounds.center;
            float searchRadius = IsLargeMonument(monument) ? 400f : 75f;

            List<BaseEntity> recyclers = new List<BaseEntity>();
            List<BaseEntity> fireBarrels = new List<BaseEntity>();
            List<BaseEntity> otherEntities = new List<BaseEntity>();

            IOEntity closestWaterWell = null;
            float minWaterWellDist = float.MaxValue;

            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                float dist = Vector3.Distance(entity.transform.position, centerPos);
                if (dist > searchRadius) continue;

                string prefabName = entity.ShortPrefabName;
                if (prefabName.Contains("recycler"))
                {
                    recyclers.Add(entity);
                }
                else if (prefabName == "fire_barrel")
                {
                    fireBarrels.Add(entity);
                }
                else if (prefabName.Contains("water_pump") || prefabName.Contains("electric_switch") || prefabName.Contains("research_table"))
                {
                    otherEntities.Add(entity);
                }
            }

            IOEntity[] ioEntities = UnityEngine.Object.FindObjectsOfType<IOEntity>();
            foreach (var ioEntity in ioEntities)
            {
                if (ioEntity.ShortPrefabName.Contains("water_well"))
                {
                    float wellDist = Vector3.Distance(ioEntity.transform.position, centerPos);
                    if (wellDist < minWaterWellDist)
                    {
                        minWaterWellDist = wellDist;
                        closestWaterWell = ioEntity;
                    }
                }
            }

            if (recyclers.Count > 0)
            {
                BaseEntity bestRecycler = recyclers.OrderBy(e => Vector3.Distance(e.transform.position, centerPos)).First();
                return SafePosition(bestRecycler.transform.position, 1f);
            }
            if (closestWaterWell != null)
            {
                return SafePosition(closestWaterWell.transform.position, 1f);
            }
            if (fireBarrels.Count > 0)
            {
                BaseEntity bestBarrel = fireBarrels.OrderBy(e => Vector3.Distance(e.transform.position, centerPos)).First();
                return SafePosition(bestBarrel.transform.position, 1f);
            }
            if (otherEntities.Count > 0)
            {
                BaseEntity bestEntity = otherEntities.OrderBy(e => Vector3.Distance(e.transform.position, centerPos)).First();
                return SafePosition(bestEntity.transform.position, 1f);
            }

            return SafePosition(centerPos, 3f);
        }

        private Vector3 SafePosition(Vector3 position, float offset)
        {
            int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
                position += new Vector3(UnityEngine.Random.Range(-offset, offset), 0, UnityEngine.Random.Range(-offset, offset));

                RaycastHit hit;
                if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out hit, 5f, LayerMask.GetMask("World")))
                {
                    position = hit.point + Vector3.up * 0.2f;
                }

                if (!Physics.CheckSphere(position, 0.5f, LayerMask.GetMask("World", "Default")))
                {
                    return position;
                }

                PrintWarning($"⚠ Попытка спавна в текстурах ({position}). Пробуем другую точку...");
                position += Vector3.forward * 1f;
            }

            return position;
        }

        private bool IsLargeMonument(MonumentInfo monument)
        {
            string name = monument.name.ToLower();
            return name.Contains("trainyard") || name.Contains("airfield") || name.Contains("powerplant") ||
                   name.Contains("military_tunnel") || name.Contains("launch_site") || name.Contains("harbor") ||
                   name.Contains("sewer") || name.Contains("water_treatment");
        }

        private bool IsBlacklisted(MonumentInfo monument)
        {
            string name = monument.name.ToLower();
            return name.Contains("canyon") || name.Contains("quarry") || name.Contains("metro") ||
                   name.Contains("oasis") || name.Contains("underwater") || name.Contains("excavator") ||
                   name.Contains("oilrig") || name.Contains("bandit") || name.Contains("safezone") ||
                   name.Contains("train tunnel") || name.Contains("lake") || name.Contains("missile silo") ||
                   name.Contains("underground cave");
        }

        private void DespawnCultist()
        {
            if (cultistNPC != null && !cultistNPC.IsDestroyed)
            {
                cultistNPC.Kill();
            }
            ScheduleNextSpawn();
        }

        [ChatCommand("cultist_spawn")]
        private void CmdForceSpawn(BasePlayer player)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }
            SpawnCultist();
            SendReply(player, "The Cultist has been spawned!");
        }

        [ChatCommand("cultist_tp")]
        private void CmdTeleportToCultist(BasePlayer player)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }
            if (cultistNPC == null || cultistNPC.IsDestroyed)
            {
                SendReply(player, "The Cultist was not found!");
                return;
            }
            player.Teleport(cultistNPC.transform.position);
            SendReply(player, "You have been teleported to the Cultist!");
        }

        private string GetMonumentName(MonumentInfo monument)
        {
            if (monument.displayPhrase != null && !string.IsNullOrEmpty(monument.displayPhrase.english))
            {
                return monument.displayPhrase.english;
            }
            return monument.name;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.USE))
            {
                if (cultistNPC == null || cultistNPC.IsDestroyed)
                    return;
                if (Vector3.Distance(player.transform.position, cultistNPC.transform.position) <= 2f)
                {
                    OpenTradeMenu(player);
                }
            }
        }

        private void OpenTradeMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CultistTradeUI");

            player.SendConsoleCommand("toggleui");

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.9" },
                RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.7 0.8" },
                CursorEnabled = true
            }, "Overlay", "CultistTradeUI");

            container.Add(new CuiLabel
            {
                Text = { Text = "Торговля с Культистом", FontSize = 20, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1" }
            }, "CultistTradeUI");

            container.Add(new CuiButton
            {
                Button = { Command = "close_trade", Color = "0.8 0 0 1" },
                RectTransform = { AnchorMin = "0.85 0.9", AnchorMax = "0.98 0.98" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, "CultistTradeUI");

            int index = 0;
            foreach (var tradeItem in tradeItems)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = $"trade_buy {tradeItem.Key}", Color = "0 0.6 0 1" },
                    RectTransform = { AnchorMin = $"0.1 {0.7 - index * 0.2}", AnchorMax = $"0.9 {0.8 - index * 0.2}" },
                    Text = { Text = $"{tradeItem.Key.ToUpper()} - {tradeItem.Value} черепов", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, "CultistTradeUI");

                index++;
            }

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("close_trade")]
        private void CmdCloseTrade(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null) return;

            CuiHelper.DestroyUi(player, "CultistTradeUI");
            player.SendConsoleCommand("toggleui");
        }

        [ConsoleCommand("trade_buy")]
        private void CmdTradeBuy(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null || arg.Args == null || arg.Args.Length == 0) return;

            string itemShortName = arg.Args[0];
            if (!tradeItems.ContainsKey(itemShortName)) return;

            int price = tradeItems[itemShortName];
            if (player.inventory.GetAmount(ItemManager.FindItemDefinition("skull.human").itemid) < price)
            {
                SendReply(player, "❌ У вас недостаточно черепов для покупки!");
                return;
            }

            player.inventory.Take(null, ItemManager.FindItemDefinition("skull.human").itemid, price);
            Item item = ItemManager.CreateByName(itemShortName, 1);
            player.GiveItem(item);

            SendReply(player, $"✅ Вы купили {itemShortName} за {price} черепов!");
        }
    }
}