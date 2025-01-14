﻿#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。

using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Web.Responses;
using ASFEnhance.Data;
using SteamKit2;
using static ASFEnhance.Utils;

namespace ASFEnhance.Wishlist
{
    internal static class WebRequest
    {
        /// <summary>
        /// 添加愿望单
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="gameID"></param>
        /// <returns></returns>
        internal static async Task<bool> AddWishlist(this Bot bot, uint gameID)
        {
            Uri request = new(SteamStoreURL, "/api/addtowishlist");
            Uri referer = new(SteamStoreURL, "/app/" + gameID);

            Dictionary<string, string> data = new(2, StringComparer.Ordinal)
            {
                { "appid", gameID.ToString() },
            };

            ObjectResponse<ResultResponse>? response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<ResultResponse>(request, data: data, referer: referer).ConfigureAwait(false);

            if (response == null)
            {
                return false;
            }

            if (response.Content.Result != EResult.OK)
            {
                bot.ArchiLogger.LogGenericWarning(Strings.WarningFailed);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 删除愿望单
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="gameID"></param>
        /// <returns></returns>
        internal static async Task<bool> RemoveWishlist(this Bot bot, uint gameID)
        {
            Uri request = new(SteamStoreURL, "/api/removefromwishlist");
            Uri referer = new(SteamStoreURL, $"/app/{gameID}");

            Dictionary<string, string> data = new(2, StringComparer.Ordinal)
            {
                { "appid", gameID.ToString() },
            };

            ObjectResponse<ResultResponse>? response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<ResultResponse>(request, data: data, referer: referer).ConfigureAwait(false);

            if (response == null)
            {
                return false;
            }

            if (response.Content.Result != EResult.OK)
            {
                bot.ArchiLogger.LogGenericWarning(Strings.WarningFailed);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 关注游戏
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="gameID"></param>
        /// <returns></returns>
        internal static async Task<bool> FollowGame(this Bot bot, uint gameID, bool isFollow)
        {
            Uri request = new(SteamStoreURL, "/explore/followgame/");
            Uri referer = new(SteamStoreURL, $"/app/{gameID}");

            Dictionary<string, string> data = new(3, StringComparer.Ordinal)
            {
                { "appid", gameID.ToString() },
            };

            if (!isFollow)
            {
                data.Add("unfollow", "1");
            }

            HtmlDocumentResponse? response = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(request, data: data, referer: referer).ConfigureAwait(false);

            if (response == null)
            {
                return false;
            }

            return response.Content.Body.InnerHtml.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 检查关注/愿望单情况
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="gameID"></param>
        /// <returns></returns>
        internal static async Task<CheckGameResponse?> CheckGame(this Bot bot, uint gameID)
        {
            Uri request = new(SteamStoreURL, $"/app/{gameID}");

            HtmlDocumentResponse? response = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(request).ConfigureAwait(false);

            if (response == null)
            {
                return new(false, "网络错误");
            }

            if (response.FinalUri.LocalPath.Equals("/"))
            {
                return new(false, "商店页未找到");
            }

            return HtmlParser.ParseStorePage(response);

        }
    }
}
