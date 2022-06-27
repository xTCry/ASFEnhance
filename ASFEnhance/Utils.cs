﻿#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。

using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ASFEnhance.Data;
using System.Reflection;

namespace ASFEnhance
{
    internal static class Utils
    {
        /// <summary>
        /// 有更新可用
        /// </summary>
        internal static bool UpdateAvilable { get; set; }
        /// <summary>
        /// 更新已就绪
        /// </summary>
        internal static bool UpdatePadding { get; set; }

        /// <summary>
        /// 更新标记
        /// </summary>
        /// <returns></returns>
        private static string UpdateFlag()
        {
            if (UpdatePadding)
            {
                return "*";
            }
            else if (UpdateAvilable)
            {
                return "!";
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 格式化返回文本
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string FormatStaticResponse(string message)
        {
            string flag = UpdateFlag();

            return $"<ASFE{flag}> {message}";
        }

        /// <summary>
        /// 格式化返回文本
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string FormatBotResponse(this Bot bot, string message)
        {
            string flag = UpdateFlag();

            return $"<{bot.BotName}{flag}> {message}";
        }

        /// <summary>
        /// 获取Bot SessionID
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static string? GetBotSessionID(this Bot bot)
        {
            string? sessionID = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(SteamStoreURL, "sessionid");

            if (string.IsNullOrEmpty(sessionID))
            {
                bot.ArchiLogger.LogNullError(nameof(sessionID));
                return null;
            }

            return sessionID;
        }

        /// <summary>
        /// 转换SteamID
        /// </summary>
        /// <param name="steamID"></param>
        /// <returns></returns>
        internal static ulong SteamID2Steam32(ulong steamID)
        {
            return steamID - 0x110000100000000;
        }

        /// <summary>
        /// 匹配Steam商店ID
        /// </summary>
        /// <param name="query"></param>
        /// <param name="defaultType"></param>
        /// <returns></returns>
        internal static Dictionary<string, SteamGameID> FetchGameIDs(string query, SteamGameIDType validType, SteamGameIDType defaultType)
        {
            Dictionary<string, SteamGameID> result = new();

            string[] entries = query.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            SteamGameID error = new(SteamGameIDType.Error, 0);

            foreach (string entry in entries)
            {
                uint gameID;
                string strType;
                int index = entry.IndexOf('/', StringComparison.Ordinal);

                if ((index > 0) && (entry.Length > index + 1))
                {
                    if (!uint.TryParse(entry[(index + 1)..], out gameID) || (gameID == 0))
                    {
                        result.Add(entry, error);
                        continue;
                    }

                    strType = entry[..index];
                }
                else if (uint.TryParse(entry, out gameID) && (gameID > 0))
                {
                    result.Add(entry, new(defaultType, gameID));
                    continue;
                }
                else
                {
                    result.Add(entry, error);
                    continue;
                }

                SteamGameIDType type = strType.ToUpperInvariant() switch {
                    "A" or "APP" => SteamGameIDType.App,
                    "S" or "SUB" => SteamGameIDType.Sub,
                    "B" or "BUNDLE" => SteamGameIDType.Bundle,
                    _ => SteamGameIDType.Error,
                };

                if (validType.HasFlag(type))
                {
                    result.Add(entry, new(type, gameID));
                }
                else
                {
                    result.Add(entry, error);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取版本号
        /// </summary>
        internal static Version MyVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0");

        /// <summary>
        /// 获取插件所在路径
        /// </summary>
        internal static string MyLocation => Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// Steam商店链接
        /// </summary>
        internal static Uri SteamStoreURL => ArchiWebHandler.SteamStoreURL;

        /// <summary>
        /// Steam社区链接
        /// </summary>
        internal static Uri SteamCommunityURL => ArchiWebHandler.SteamCommunityURL;

        /// <summary>
        /// 日志
        /// </summary>
        internal static ArchiLogger ASFLogger => ASF.ArchiLogger;
    }
}
