﻿using Marketplace.Modules.Banker;
using Marketplace.Modules.Global_Options;
using Marketplace.Paths;

namespace Marketplace.Modules.MainMarketplace;

[UsedImplicitly]
[Market_Autoload(Market_Autoload.Type.Server, Market_Autoload.Priority.Normal)]
public static class Marketplace_Main_Server
{
    private static Dictionary<string, int> PlayersIncome = new();

    [UsedImplicitly]
    private static void OnInit()
    {
        if (!File.Exists(Market_Paths.ServerMarketDataJSON)) File.Create(Market_Paths.ServerMarketDataJSON).Dispose();
        string data = Market_Paths.ServerMarketDataJSON.ReadFile();
        if (!string.IsNullOrWhiteSpace(data))
            Marketplace_DataTypes.SyncedMarketplaceData.Value =
                JSON.ToObject<List<Marketplace_DataTypes.ServerMarketSendData>>(data);

        if (!File.Exists(Market_Paths.MarketPlayersIncomeJSON))
            File.Create(Market_Paths.MarketPlayersIncomeJSON).Dispose();
        string goldData = Market_Paths.MarketPlayersIncomeJSON.ReadFile();
        if (!string.IsNullOrWhiteSpace(goldData)) PlayersIncome = JSON.ToObject<Dictionary<string, int>>(goldData);

        if (!File.Exists(Market_Paths.MarketPlayerMessagesJSON))
            File.Create(Market_Paths.MarketPlayerMessagesJSON).Dispose();
        string messagesData = Market_Paths.MarketPlayerMessagesJSON.ReadFile();
        if (!string.IsNullOrWhiteSpace(messagesData))
            Marketplace_Messages.Messenger.PlayerMessages = JSON.ToObject<Dictionary<string, string>>(messagesData);
    }

    private static void SavePlayersIncomeAndSendToClients(ZNetPeer target = null)
    {
        Market_Paths.MarketPlayersIncomeJSON.WriteFile(JSON.ToJSON(PlayersIncome));

        if (target != null)
        {
            string userID = target.m_socket.GetHostName();
            if (!PlayersIncome.ContainsKey(userID)) PlayersIncome[userID] = 0;
            ZRoutedRpc.instance.InvokeRoutedRPC(target.m_uid, "KGmarket ReceiveIncome", PlayersIncome[userID]);
            return;
        }

        foreach (ZNetPeer peer in ZNet.instance.m_peers)
        {
            string userID = peer.m_socket.GetHostName();
            if (!PlayersIncome.ContainsKey(userID)) PlayersIncome[userID] = 0;
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "KGmarket ReceiveIncome", PlayersIncome[userID]);
        }
    }

    public static void SendMessagesToClient(string userID)
    {
        if (!Marketplace_Messages.Messenger.PlayerMessages.ContainsKey(userID)) return;
        ZNetPeer tryGetPeer = ZNet.instance.GetPeerByHostName(userID);
        if (tryGetPeer == null) return;
        ZPackage pkg = new ZPackage();
        pkg.Write(Marketplace_Messages.Messenger.PlayerMessages[userID]);
        pkg.Compress();
        ZRoutedRpc.instance.InvokeRoutedRPC(tryGetPeer.m_uid, "KGmarket GetLocalMessages", pkg);
    }

    private static void SendIncomeToClient(ZNetPeer peer, string userID)
    {
        if (!PlayersIncome.ContainsKey(userID)) PlayersIncome[userID] = 0;
        ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "KGmarket ReceiveIncome", PlayersIncome[userID]);
    }

    private static void RequestWithdrawIncome(long sender, bool toBank)
    {
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        if (peer == null) return;
        string userID = peer.m_socket.GetHostName();
        int value = 0;
        if (PlayersIncome.TryGetValue(userID, out int gold)) value = gold;
        PlayersIncome[userID] = 0;
        SavePlayersIncomeAndSendToClients(peer);
        if (value <= 0) return;
        Market_Logger.Log(Market_Logger.LogType.Marketplace,
            $"Player User ID: {userID} requested Withdraw Marketplace with quantity: {value}");
        if (toBank)
        {
            Banker_Main_Server.MethodBankerDeposit(sender, Global_Configs.SyncedGlobalOptions.Value._serverCurrency,
                value);
            return;
        }

        Marketplace_DataTypes.ServerMarketSendData sendMoney = new Marketplace_DataTypes.ServerMarketSendData
            { Count = value, ItemPrefab = Global_Configs.SyncedGlobalOptions.Value._serverCurrency, Quality = 1 };
        string json = JSON.ToJSON(sendMoney);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, "KGmarket BuyItemAnswer", json);
    }

    private static void ReceiveItemFromClient(long sender, string data)
    {
        if (data.Length <= 0) return;
        Marketplace_DataTypes.ClientMarketSendData toConvert =
            JSON.ToObject<Marketplace_DataTypes.ClientMarketSendData>(data);
        if (toConvert.Count <= 0) return;
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        if (peer == null) return;
        string userID = peer.m_socket.GetHostName();
        Marketplace_DataTypes.ServerMarketSendData newData =
            new Marketplace_DataTypes.ServerMarketSendData(toConvert, userID);
        Marketplace_DataTypes.SyncedMarketplaceData.Value.Add(newData);
        SaveMarketAndSendToClients();
        Marketplace_Messages.Messenger.PostNewItemMessage(userID, newData);
        Market_Logger.Log(Market_Logger.LogType.Marketplace,
            $"Player User ID: {userID} added an item {newData.ItemPrefab} with quantity: {newData.Count} price: {newData.Price}");
        DiscordStuff.DiscordStuff.SendMarketplaceWebhook(newData);
    }

    private static void SaveMarketAndSendToClients()
    {
        Market_Paths.ServerMarketDataJSON.WriteFile(JSON.ToJSON(Marketplace_DataTypes.SyncedMarketplaceData.Value));
        Marketplace_DataTypes.SyncedMarketplaceData.Update();
    }

    private static void RemoveItemAdminStatus(long sender, int id)
    {
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        if (peer == null) return;
        string userID = peer.m_socket.GetHostName();
        if (ZNet.instance.m_adminList == null ||
            !ZNet.instance.ListContainsId(ZNet.instance.m_adminList, userID)) return;
        Marketplace_DataTypes.ServerMarketSendData findData =
            Marketplace_DataTypes.SyncedMarketplaceData.Value.Find(data => data.UID == id);
        if (findData == null) return;
        Marketplace_DataTypes.SyncedMarketplaceData.Value.Remove(findData);
        SaveMarketAndSendToClients();
    }

    private static void RequestBuyItem(long sender, int id, int goldValue, int quantity)
    {
        Marketplace_DataTypes.ServerMarketSendData findData =
            Marketplace_DataTypes.SyncedMarketplaceData.Value.Find(data => data.UID == id);
        if (findData != null)
        {
            if (quantity >= findData.Count)
            {
                Marketplace_DataTypes.SyncedMarketplaceData.Value.Remove(findData);
                string json = JSON.ToJSON(findData);
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "KGmarket BuyItemAnswer", json);
                int leftOver = quantity - findData.Count;
                goldValue = findData.Count * findData.Price;
                if (leftOver > 0)
                {
                    Marketplace_DataTypes.ServerMarketSendData mockData = new Marketplace_DataTypes.ServerMarketSendData
                    {
                        Count = leftOver * findData.Price,
                        ItemPrefab = Global_Configs.SyncedGlobalOptions.Value._serverCurrency,
                        Quality = 1
                    };
                    string jsonLeftOver = JSON.ToJSON(mockData);
                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, "KGmarket BuyItemAnswer", jsonLeftOver);
                }
            }
            else
            {
                int needToSet = findData.Count - quantity;
                findData.Count = quantity;
                string json = JSON.ToJSON(findData);
                findData.Count = needToSet;
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "KGmarket BuyItemAnswer", json);
            }

            SaveMarketAndSendToClients();
            ///////////income mechanics
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer == null) return;
            string buyerUserID = peer.m_socket.GetHostName();
            string sellerUserID = findData.SellerUserID!;

            if (sellerUserID == buyerUserID)
                return;

            string buyerName = peer.m_playerName;

            int applyTaxes = Global_Configs.SyncedGlobalOptions.Value._vipPlayerList.Contains(sellerUserID)
                ? Global_Configs.SyncedGlobalOptions.Value._vipmarketTaxes
                : Global_Configs.SyncedGlobalOptions.Value._marketTaxes;
            applyTaxes = Mathf.Max(0, applyTaxes);
            float endValue = goldValue - goldValue * (applyTaxes / 100f);
            if (PlayersIncome.ContainsKey(sellerUserID))
                PlayersIncome[sellerUserID] =
                    (int)Math.Min(int.MaxValue, (long)PlayersIncome[sellerUserID] + (int)endValue);
            else
                PlayersIncome[sellerUserID] = (int)Math.Min(int.MaxValue, (long)endValue);

            SavePlayersIncomeAndSendToClients();
            Marketplace_Messages.Messenger.BuyCancelMessage(buyerUserID, sellerUserID, findData, quantity);
            Market_Logger.Log(Market_Logger.LogType.Marketplace,
                buyerUserID == sellerUserID
                    ? $"{buyerName} (ID: {buyerUserID}) cancelled his slot {findData.ItemPrefab} quantity: {findData.Count} price: {findData.Price}. He took: {quantity} items"
                    : $"{buyerName} (ID: {buyerUserID}) bought {findData.SellerName}'s (ID: {sellerUserID}) slot {findData.ItemPrefab} quantity: {findData.Count} (bought: x{quantity}) price: {findData.Price}");
            ///////////////////////////
        }
        else
        {
            Marketplace_DataTypes.ServerMarketSendData mockData = new Marketplace_DataTypes.ServerMarketSendData
            {
                Count = goldValue, ItemPrefab = Global_Configs.SyncedGlobalOptions.Value._serverCurrency, Quality = 1
            };
            string json = JSON.ToJSON(mockData);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, "KGmarket BuyItemAnswer", json);
        }
    }


    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [ServerOnlyPatch]
    private static class ZrouteMethodsServer
    {
        [UsedImplicitly]
        private static void Postfix()
        {
            if (!ZNet.instance.IsServer()) return;
            ZRoutedRpc.instance.Register("KGmarket ReceiveItem", new Action<long, string>(ReceiveItemFromClient));
            ZRoutedRpc.instance.Register("KGmarket RequestBuyItem", new Action<long, int, int, int>(RequestBuyItem));
            ZRoutedRpc.instance.Register("KGmarket RequestWithdraw", new Action<long, bool>(RequestWithdrawIncome));
            ZRoutedRpc.instance.Register("KGmarket RemoveItemAdmin", new Action<long, int>(RemoveItemAdminStatus));
            ZRoutedRpc.instance.Register("KGmarket ClearMessages", Marketplace_Messages.Messenger.ClearPlayerMessages);
        }
    }


    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_CharacterID))]
    [ServerOnlyPatch]
    private static class ZnetSyncJson
    {
        [UsedImplicitly]
        private static void Postfix(ZRpc rpc)
        {
            if (!ZNet.instance.IsServer()) return;
            ZNetPeer peer = ZNet.instance.GetPeer(rpc);
            if (peer == null) return;
            string userID = peer.m_socket.GetHostName();
            SendIncomeToClient(peer, userID);
            SendMessagesToClient(userID);
        }
    }
}