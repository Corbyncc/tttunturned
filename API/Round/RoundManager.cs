﻿using SDG.Unturned;
using System;
using System.Threading.Tasks;
using TTTUnturned.API.Core;
using TTTUnturned.API.Interface;
using TTTUnturned.API.Lobby;
using TTTUnturned.API.Roles;
using TTTUnturned.Utils;
using UnityEngine;

namespace TTTUnturned.API.Round
{
    public class RoundManager : MonoBehaviour, IObjectComponent
    {
        public static LobbySession Lobby;

        public void Awake()
        {
            Lobby = LobbyManager.GetLobby();

            CommandWindow.Log("RoundManager loaded");

            AsyncHelper.Schedule("RoundTick", RoundTick, 1000);
        }

        public static void CheckWin()
        {
            Task.Run(async () =>
            {
                if (Lobby.GetAlive(PlayerRole.TERRORIST).Count == 0)
                {
                    CommandWindow.Log("Innocents win");
                    LobbyManager.Message("<color=lime>Innocents</color> Win!");
                    await InterfaceManager.SendLobbyBannerMessage(8493, $"Innocents Win!", 10000, true);
                    await Lobby.Stop();

                    // Innocents win
                }
                if (Lobby.GetAlive(PlayerRole.DETECTIVE).Count == 0 && Lobby.GetAlive(PlayerRole.INNOCENT).Count == 0)
                {
                    CommandWindow.Log("Terrorist win");
                    LobbyManager.Message("<color=red>Terroists</color> Win!");
                    await InterfaceManager.SendLobbyBannerMessage(8492, $"Terroists Win!", 10000, true);
                    await Lobby.Stop();
                    // Terrorist win
                }
            });
        }

        private async Task RoundTick()
        {
            LobbySession lobby = LobbyManager.GetLobby();

            if (lobby.State != LobbyState.LIVE) return;

            lobby.RoundTime--;
            if(Main.Config.DebugMode) CommandWindow.Log(lobby.RoundTime);

            lobby.Players.ForEach(async player =>
            {
                await InterfaceManager.SendUIEffectTextAsync(8490, player.SteamID, true, "TimerValue", ParseTime(lobby.RoundTime));
               // await SendUIEffectAsync();
            });

            if (lobby.RoundTime == 60) // Convert this to updating the time displayed on the ui once added.
            {
                LobbyManager.Message("1 minute remaining");
            }

            if (lobby.RoundTime == 0)
            {
                CommandWindow.Log("Innocents win");
                LobbyManager.Message("<color=lime>Innocents</color> Win!");

                await Lobby.Stop();
                //AsyncHelper.RunAsync("LobbyStopRoundTime", Lobby.Stop);
                return;
            }
        }

        public static string ParseTime(int seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.ToString(@"mm\:ss");
        }
    }
}
