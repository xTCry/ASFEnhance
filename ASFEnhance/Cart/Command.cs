#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。

using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;

using Chrxw.ASFEnhance.Data;
using Chrxw.ASFEnhance.Localization;

using SteamKit2;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Chrxw.ASFEnhance.Cart.Response;
using static Chrxw.ASFEnhance.Utils;


namespace Chrxw.ASFEnhance.Cart
{
    internal static class Command
    {
        /// <summary>
        /// 读取购物车
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponseGetCartGames(Bot bot, EAccess access)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Operator)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            CartResponse cartResponse = await WebRequest.GetCartGames(bot).ConfigureAwait(false);

            StringBuilder response = new();

            string walletCurrency = bot.WalletCurrency != ECurrencyCode.Invalid ? bot.WalletCurrency.ToString() : string.Format(CurrentCulture, Langs.WalletAreaUnknown);

            if (cartResponse.cartData.Count > 0)
            {
                response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartTotalPrice, cartResponse.totalPrice / 100.0, walletCurrency)));

                foreach (CartData cartItem in cartResponse.cartData)
                {
                    response.AppendLine(string.Format(CurrentCulture, Langs.CartItemInfo, cartItem.path, cartItem.name, cartItem.price / 100.0));
                }

                response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartPurchaseSelf, cartResponse.purchaseSelf ? "√" : "×")));
                response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartPurchaseGift, cartResponse.purchaseGift ? "√" : "×")));
            }
            else
            {
                response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartIsEmpty)));
            }

            return response.Length > 0 ? response.ToString() : null;
        }

        /// <summary>
        /// 读取购物车 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseGetCartGames(EAccess access, string botNames)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGetCartGames(bot, access))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 添加商品到购物车
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponseAddCartGames(Bot bot, EAccess access, string query)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Operator)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            string[] entries = query.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder response = new();

            foreach (string entry in entries)
            {
                uint gameID;
                string type;

                int index = entry.IndexOf('/', StringComparison.Ordinal);

                if ((index > 0) && (entry.Length > index + 1))
                {
                    if (!uint.TryParse(entry[(index + 1)..], out gameID) || (gameID == 0))
                    {
                        response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Strings.ErrorIsInvalid, nameof(gameID))));
                        continue;
                    }

                    type = entry[..index];
                }
                else if (uint.TryParse(entry, out gameID) && (gameID > 0))
                {
                    type = "SUB";
                }
                else
                {
                    response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Strings.ErrorIsInvalid, nameof(gameID))));
                    continue;
                }

                bool? result;

                switch (type.ToUpperInvariant())
                {
                    case "S":
                    case "SUB":
                        result = await WebRequest.AddCert(bot, gameID, false).ConfigureAwait(false);
                        break;
                    case "B":
                    case "BUNDLE":
                        result = await WebRequest.AddCert(bot, gameID, true).ConfigureAwait(false);
                        break;
                    default:
                        response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartInvalidType, entry)));
                        continue;
                }

                if (result != null)
                {
                    response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Strings.BotAddLicense, entry, (bool)result ? EResult.OK : EResult.Fail)));
                }
                else
                {
                    response.AppendLine(FormatBotResponse(bot, string.Format(CurrentCulture, Strings.BotAddLicense, entry, string.Format(CurrentCulture, Langs.CartNetworkError))));
                }
            }
            return response.Length > 0 ? response.ToString() : null;
        }

        /// <summary>
        /// 添加商品到购物车 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseAddCartGames(EAccess access, string botNames, string query)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAddCartGames(bot, access, query))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 清空购物车
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponseClearCartGames(Bot bot, EAccess access)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Operator)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            bool? result = await WebRequest.ClearCert(bot).ConfigureAwait(false);

            if (result == null)
            {
                return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartEmptyResponse));
            }

            return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.CartResetResult, (bool)result ? Langs.Success : Langs.Failure));
        }

        /// <summary>
        /// 清空购物车 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseClearCartGames(EAccess access, string botNames)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseClearCartGames(bot, access))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 获取购物车可用区域
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponseGetCartCountries(Bot bot, EAccess access)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Operator)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            List<CartCountryData> result = await WebRequest.CartGetCountries(bot).ConfigureAwait(false);

            if (result.Count == 0)
            {
                return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.NoAvailableArea));
            }

            StringBuilder response = new(string.Format(CurrentCulture, Langs.AvailableAreaHeader));

            foreach (CartCountryData cc in result)
            {
                if (cc.current)
                {
                    response.AppendLine(string.Format(CurrentCulture, Langs.AreaItemCurrent, cc.code, cc.name));
                }
                else
                {
                    response.AppendLine(string.Format(CurrentCulture, Langs.AreaItem, cc.code, cc.name));
                }
            }

            return FormatBotResponse(bot, response.ToString());
        }

        /// <summary>
        /// 获取购物车可用区域 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseGetCartCountries(EAccess access, string botNames)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGetCartCountries(bot, access))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        // TODO
        /// <summary>
        /// 购物车改区
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponseSetCountry(Bot bot, EAccess access, string countryCode)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Operator)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            bool result = await WebRequest.CartSetCountry(bot, countryCode).ConfigureAwait(false);

            return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.SetCurrentCountry, result ? Langs.Success : Langs.Failure));
        }

        // TODO
        /// <summary>
        /// 购物车改区 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseSetCountry(EAccess access, string botNames, string countryCode)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSetCountry(bot, access, countryCode))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 购物车下单
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        internal static async Task<string?> ResponsePurchase(Bot bot, EAccess access)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (access < EAccess.Master)
            {
                return null;
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return FormatBotResponse(bot, Strings.BotNotConnected);
            }

            HtmlDocumentResponse? response1 = await WebRequest.CheckOut(bot, false).ConfigureAwait(false);

            if (response1 == null)
            {
                return string.Format(CurrentCulture, Langs.PurchaseCartFailureEmpty);
            }

            ObjectResponse<PurchaseResponse?> response2 = await WebRequest.InitTransaction(bot).ConfigureAwait(false);

            if (response2 == null)
            {
                return string.Format(CurrentCulture, Langs.PurchaseCartFailureFinalizeTransactionIsNull);
            }

            string? transID = response2.Content.TransID ?? response2.Content.TransActionID;

            if (string.IsNullOrEmpty(transID))
            {
                return string.Format(CurrentCulture, Langs.PurchaseCartTransIDIsNull);
            }

            ObjectResponse<FinalPriceResponse?> response3 = await WebRequest.GetFinalPrice(bot, transID, false).ConfigureAwait(false);

            if (response3 == null || response2.Content.TransID == null)
            {
                return string.Format(CurrentCulture, Langs.PurchaseCartGetFinalPriceIsNull);
            }

            float OldBalance = bot.WalletBalance;

            ObjectResponse<TransactionStatusResponse?> response4 = await WebRequest.FinalizeTransaction(bot, transID).ConfigureAwait(false);

            if (response4 == null)
            {
                return string.Format(CurrentCulture, Langs.PurchaseCartFailureFinalizeTransactionIsNull);
            }

            await Task.Delay(2000).ConfigureAwait(false);

            float nowBalance = bot.WalletBalance;

            if (nowBalance < OldBalance)
            {
                //成功购买之后自动清空购物车
                await WebRequest.ClearCert(bot).ConfigureAwait(false);

                return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.PurchaseDone, response4.Content.PurchaseReceipt.FormattedTotal));
            }
            else
            {
                return FormatBotResponse(bot, string.Format(CurrentCulture, Langs.PurchaseFailed));
            }
        }

        /// <summary>
        /// 购物车下单 (多个Bot)
        /// </summary>
        /// <param name="access"></param>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponsePurchase(EAccess access, string botNames)
        {
            if (!Enum.IsDefined(access))
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
            }

            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if ((bots == null) || (bots.Count == 0))
            {
                return access >= EAccess.Owner ? FormatStaticResponse(string.Format(CurrentCulture, Strings.BotNotFound, botNames)) : null;
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponsePurchase(bot, access))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }
    }
}
