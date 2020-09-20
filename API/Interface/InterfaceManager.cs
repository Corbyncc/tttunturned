﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TTTUnturned.API.Core;
using UnityEngine;
using System.Collections.Generic;
using TTTUnturned.API.Players;
using PlayerManager = TTTUnturned.API.Players.PlayerManager;
using TTTUnturned.API.Round;
using TTTUnturned.API.Roles;
using TTTUnturned.Utils;

namespace TTTUnturned.API.Interface
{
    public class InterfaceManager : MonoBehaviour, IObjectComponent
    {
        private static Dictionary<CSteamID, long> KeyCooldowns;
        private static List<CSteamID> UIToggled;

        public void Awake()
        {
            CommandWindow.Log("InterfaceManager loaded");

            KeyCooldowns = new Dictionary<CSteamID, long>();
            UIToggled = new List<CSteamID>();

            EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
            PlayerInput.onPluginKeyTick += OnPluginKeyTick;
            Provider.onEnemyConnected += OnEnemyConnected;
        }

        #region API
        public static async Task SendBannerMessage(CSteamID steamId, ushort id, string message, int duration, bool reliable)
        {
            SendUIEffectUnsafe(id, 8480, steamId, reliable);
            SendUIEffectTextUnsafe(8480, steamId, true, "RoleValue", message);
            await Task.Delay(duration);
            ClearUIEffectUnsafe(id, steamId);
        }

        public static void ClearUIEffectUnsafe(ushort id, CSteamID steamID)
        {
            UnityThread.executeCoroutine(ClearUIEffectCoroutine(id, steamID));
        }

        public static void SendUIEffectUnsafe(ushort id, short key, CSteamID steamID, bool reliable)
        {
            UnityThread.executeCoroutine(SendUIEffectCoroutine(id, key, steamID, reliable));
        }

        public static void SendUIEffectTextUnsafe(short key, CSteamID steamID, bool reliable, string component, string text)
        {
            UnityThread.executeCoroutine(SendUIEffectTextCoroutine(key, steamID, reliable, component, text));
        }

        public static void DisableHUDUnsafe(CSteamID steamID)
        {
            UnityThread.executeCoroutine(DisableHUDCoroutine(steamID));
        }

        #endregion

        #region Events
        public void OnEffectButtonClicked(Player player, string buttonName)
        {
            TTTPlayer tttPlayer = PlayerManager.GetTTTPlayer(player.channel.owner.playerID.steamID);
            if (buttonName.Substring(0, 2) == "T_")
            {
                switch (buttonName.Remove(0, 2))
                {
                    case "ChargeButton":
                        player.inventory.forceAddItem(new Item(1241, true), true);
                        tttPlayer.SendMessage("You redeemed C4");
                        break;
                    case "CoughSyrupButton":
                        player.inventory.forceAddItem(new Item(15, true), true);
                        tttPlayer.SendMessage("You redeemed Medkit");
                        break;
                    case "KnifeButton":
                        player.inventory.forceAddItem(new Item(140, true), true);
                        tttPlayer.SendMessage("You redeemed Knife");
                        break;
                    case "LMGButton":
                        player.inventory.forceAddItem(new Item(126, true), true);
                        tttPlayer.SendMessage("You redeemed LMG");
                        break;
                    case "SupressedPistol":
                        //player.inventory.forceAddItem(SilencedPistol.Create(), true);
                        tttPlayer.SendMessage("You redeemed Suppresed Pistol");
                        break;
                    case "BombVestButton":
                        player.inventory.forceAddItem(new Item(1013, true), true);
                        tttPlayer.SendMessage("You redeemed Bomb Vest");
                        break;
                    case "BodyArmourButton":
                        PlayerManager.GetTTTPlayer(player.channel.owner.playerID.steamID).Armor = true;
                        tttPlayer.SendMessage("You redeemed Armor Vest");
                        break;
                }
            }
        }

        private void OnEnemyConnected(SteamPlayer steamPlayer)
        {
            DisableHUDUnsafe(steamPlayer.playerID.steamID);
            SendUIEffectUnsafe(8498, 8490, steamPlayer.playerID.steamID, true);
            SendUIEffectTextUnsafe(8490, steamPlayer.playerID.steamID, true, "RoleValue", "WAITING");
            SendUIEffectTextUnsafe(8490, steamPlayer.playerID.steamID, true, "TimerValue", "00:00");
        }

        private void OnPluginKeyTick(Player player, uint simulation, byte key, bool state)
        {
            if (!state || key != 0) return;
            TTTPlayer tttPlayer = PlayerManager.GetTTTPlayer(player.channel.owner.playerID.steamID);
            if (tttPlayer is null) return;

            if (tttPlayer.GetStatus() == PlayerStatus.DEAD) return;
            if (RoundManager.GetState() != RoundState.LIVE) return;


            if (KeyCooldowns.ContainsKey(player.channel.owner.playerID.steamID))
            {
                long lastPressed = KeyCooldowns[player.channel.owner.playerID.steamID];
                // 1 second key cooldown on menu
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastPressed < 300) return;

                KeyCooldowns.Remove(player.channel.owner.playerID.steamID);
            }

            KeyCooldowns.Add(player.channel.owner.playerID.steamID, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (tttPlayer.Role == PlayerRole.TRAITOR)
            {
                if (UIToggled.Contains(tttPlayer.SteamID))
                {
                    UIToggled.Remove(tttPlayer.SteamID);
                    ClearUIEffectUnsafe(8501, tttPlayer.SteamID);
                    player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
                    player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, false);
                }
                else
                {
                    UIToggled.Add(tttPlayer.SteamID);
                    SendUIEffectUnsafe(8501, 8470, tttPlayer.SteamID, true);
                    player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
                    player.setPluginWidgetFlag(EPluginWidgetFlags.ForceBlur, true);
                }

            }
        }

        #endregion

        #region Coroutines
        private static IEnumerator SendEffectCoroutine(ushort id, byte x, byte y, byte z, Vector3 position)
        {
            EffectManager.sendEffect(id, x, y, z, position);
            yield return null;
        }
        private static IEnumerator SendUIEffectTextCoroutine(short key, CSteamID steamID, bool reliable, string component, string text)
        {
            EffectManager.sendUIEffectText(key, steamID, reliable, component, text);
            yield return null;
        }
        private static IEnumerator ClearUIEffectCoroutine(ushort id, CSteamID steamID)
        {
            EffectManager.askEffectClearByID(id, steamID);
            yield return null;
        }
        private static IEnumerator SendUIEffectCoroutine(ushort id, short key, CSteamID steamID, bool reliable)
        {
            EffectManager.sendUIEffect(id, key, steamID, reliable);
            yield return null;
        }
        private static IEnumerator DisableHUDCoroutine(CSteamID steamID)
        {
            Player ply = PlayerTool.getPlayer(steamID);
            if (ply is null) yield return null;

            ply.setPluginWidgetFlag(EPluginWidgetFlags.ShowFood, false);
            ply.setPluginWidgetFlag(EPluginWidgetFlags.ShowWater, false);
            ply.setPluginWidgetFlag(EPluginWidgetFlags.ShowVirus, false);
            ply.setPluginWidgetFlag(EPluginWidgetFlags.ShowOxygen, false);
            ply.setPluginWidgetFlag(EPluginWidgetFlags.ShowStatusIcons, false);
            yield return null;
        }
        #endregion
    }
}