﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Serilog;
using TRUEbot.Bot.Extensions;
using TRUEbot.Bot.Services;

namespace TRUEbot.Bot.Modules
{
    [UsedImplicitly]
    public class GeneralModule : ModuleBase
    {
        private readonly IPlayerService _playerService;

        public GeneralModule(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [Command("stats"), Summary("Gets the number of players a user has reported")]
        [UsedImplicitly]
        public Task Stats() => Stats(Context.User.Username);

        [Command("stats"), Summary("Gets the number of players a user has reported")]
        [UsedImplicitly]
        public async Task Stats(string username)
        {
            try
            {
                var players = await _playerService.GetPlayersReportedByUserAsync(username);

                if (!players.Any())
                {
                    await ReplyAsync($"{username} hasn't reported any players yet");
                    return;
                }

                var statsEmbed = BuildEmbed(username, players);

                foreach (var embedBuilder in statsEmbed)
                {
                    await ReplyAsync(embed: embedBuilder.Build());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed getting reported players for user {username}", username);
            }
        }

        [Command("spot"), Summary("Spots players and updates their location")]
        [UsedImplicitly]
        public async Task Get(string playerName, string location)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    await ReplyAsync("Invalid player name. Try !spot \"player\" \"system\"");

                    return;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    await ReplyAsync("Invalid location name. Try !spot \"player\" \"system\"");

                    return;
                }


                var response = await _playerService.TryUpdatePlayerLocation(playerName, location);

                switch (response)
                {
                    case UpdatePlayerResult.OK:
                        await Context.AddConfirmation();
                        break;
                    case UpdatePlayerResult.CantFindPlayer:
                        await ReplyAsync("Unable to find a player with that name");
                        break;
                    case UpdatePlayerResult.CantFindSystem:
                        await ReplyAsync("Unable to find a system with that name");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed spotting player {name} to in location: {location}", playerName, location);
            }
        }

        [Command("missing"), Summary("Spots players and updates their location")]
        [UsedImplicitly]
        public async Task Missing(string playerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    await ReplyAsync("Invalid player name. Try !spot \"player\" \"system\"");

                    return;
                }
                

                var response = await _playerService.TryUpdatePlayerLocation(playerName, null);

                switch (response)
                {
                    case UpdatePlayerResult.OK:
                        await Context.AddConfirmation();
                        break;
                    case UpdatePlayerResult.CantFindPlayer:
                        await ReplyAsync("Unable to find a player with that name");
                        break;
                    case UpdatePlayerResult.CantFindSystem:
                        await ReplyAsync("Unable to find a system with that name");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed spotting player {name} to in location: {location}", playerName);
            }
        }


        [Command("normalise"), Summary("Normalises system names")]
        [UsedImplicitly]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Normalise()
        {
            try
            {
                var response = await _playerService.NormaliseSystemNames();

                if (response)
                {
                    await Context.AddConfirmation();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed normalizing names");
            }
        }

      

        [Command("all"), Summary("PM's user with all the users in the database")]
        [UsedImplicitly]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task All()
        {
            try
            {

                var players = await _playerService.GetAllPlayersAsync();
                
                var playerEmbed = BuildAllPlayersEmbed(players);

                foreach (var embedBuilder in playerEmbed)
                {
                await Context.User.SendMessageAsync(embed: embedBuilder.Build());

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed getting all players ");
            }
        }
        
        private static List<EmbedBuilder> BuildAllPlayersEmbed( List<PlayerDto> players)
        {
            const int LIMIT = 950;

            var pageText = "";
            var builders = new List<EmbedBuilder>();

            foreach (var playerText in players.OrderBy(a => a.Name).Select(x => $"{x.Name} [{x.Alliance ?? "Unknown"}] ({x.Location ?? "Unknown"})"))
            {
                if ((pageText + Environment.NewLine + playerText).Length > LIMIT)
                {
                    pageText += Environment.NewLine + playerText;

                    var embed = new EmbedBuilder()
                        .WithTitle("All Players Page ");
         
                    embed.AddField("Players", pageText);

                    embed.WithFooter($"{players.Count} players").WithColor(new Color(95, 186, 125));


                    builders.Add(embed);

                    pageText = "";
                }
                else
                {
                    pageText += Environment.NewLine + playerText;
                }
            }

            var finalEmbed = new EmbedBuilder()
                .WithTitle("All Players");
         
            finalEmbed.AddField("Players", pageText);

            finalEmbed.WithFooter($"{players.Count} players").WithColor(new Color(95, 186, 125));
            
            builders.Add(finalEmbed);

            var page = 1;
            var pages = builders.Count;

            foreach (var embedBuilder in builders)
            {
                embedBuilder.Title += $"{page} of {pages}";
            }
         
            return builders;
        }

        private static List<EmbedBuilder> BuildEmbed(string username, List<PlayerDto> players)
        {
            const int LIMIT = 1020;

            var pageText = "";
            var builders = new List<EmbedBuilder>();

            foreach (var x in players.OrderByDescending(a => a.Name))
            {
                var text = $"{x.Name} ({x.Alliance ?? "Unknown"})";

                if (pageText.Length + text.Length > LIMIT)
                {
                    var embed = new EmbedBuilder();

                    embed.AddField($"Players Added by {username}", pageText);


                    embed.WithFooter($"{players.Count} players").WithColor(new Color(95, 186, 125));


                    builders.Add(embed);

                    pageText = text;
                }
                else
                {
                    pageText += Environment.NewLine + text;
                }

            }

            var finalEmbed = new EmbedBuilder();

            finalEmbed.AddField($"Players Added by {username}", pageText);
            
            finalEmbed.WithFooter($"{players.Count} players").WithColor(new Color(95, 186, 125));


            builders.Add(finalEmbed);

            var page = 1;
            var pages = builders.Count;

            foreach (var embedBuilder in builders)
            {
                embedBuilder.Title += $"{page++} of {pages}";
            }

            return builders;
        }

    }
}
