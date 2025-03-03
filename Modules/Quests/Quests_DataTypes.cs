﻿using Marketplace.ExternalLoads;
using Marketplace.Modules.Global_Options;
using Marketplace.Modules.Leaderboard;
using Marketplace.Modules.NPC;
using Marketplace.Modules.Dialogues;
using Marketplace.OtherModsAPIs;

namespace Marketplace.Modules.Quests;

public static class Quests_DataTypes
{
    internal static readonly CustomSyncedValue<int> SyncedQuestRevision =
        new(Marketplace.configSync, "questRevision", 0, CustomSyncedValueBase.Config_Priority.First);
    
    internal static readonly CustomSyncedValue<Dictionary<int, Quest>> SyncedQuestData =
        new(Marketplace.configSync, "questData", new Dictionary<int, Quest>());

    internal static readonly CustomSyncedValue<Dictionary<string, List<int>>> SyncedQuestProfiles =
        new(Marketplace.configSync, "questProfiles", new Dictionary<string, List<int>>());

    internal static readonly CustomSyncedValue<Dictionary<int, List<QuestEvent>>> SyncedQuestsEvents =
        new(Marketplace.configSync, "questEvents", new Dictionary<int, List<QuestEvent>>());

    public static readonly Dictionary<int, Quest> AllQuests = new();
    public static readonly Dictionary<int, Quest> AcceptedQuests = new(20);

    public enum QuestEventCondition
    {
        OnAcceptQuest,
        OnCancelQuest,
        OnCompleteQuest
    }

    [Flags]
    public enum SpecialQuestTag
    {
        None = 0,
        Autocomplete = 1
    }

    public enum QuestEventAction
    {
        GiveItem,
        GiveQuest,
        RemoveQuest,
        Spawn,
        Teleport,
        Damage,
        Heal,
        PlaySound,
        NpcText,
    }

    public enum QuestType
    {
        Kill,
        Collect,
        Harvest,
        Craft,
        Talk,
        Build
    }

    public enum QuestRewardType
    {
        Item,
        Skill,
        Skill_EXP,
        Pet,
        EpicMMO_EXP,
        MH_EXP,
        Cozyheim_EXP,
        SetCustomValue,
        AddCustomValue,
        GuildAddLevel,
        Battlepass_EXP
    }


    public class QuestEvent : ISerializableParameter
    {
        public QuestEventCondition cond;
        public QuestEventAction action;
        public string args;

        public QuestEvent()
        {
            
        }
        
        public QuestEvent(QuestEventCondition cond, QuestEventAction action, string args)
        {
            this.cond = cond;
            this.action = action;
            this.args = args;
        }

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write((int)cond);
            pkg.Write((int)action);
            pkg.Write(args ?? "");
        }

        public void Deserialize(ref ZPackage pkg)
        {
            cond = (QuestEventCondition)pkg.ReadInt();
            action = (QuestEventAction)pkg.ReadInt();
            args = pkg.ReadString();
        }
    }

    //serialization part
    public partial class Quest : ISerializableParameter
    {
        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write((int)Type);
            pkg.Write((int)SpecialTag);
            pkg.Write(Name ?? "");
            pkg.Write(Description ?? "");
            pkg.Write(TargetAmount);
            for (int i = 0; i < TargetAmount; ++i)
            {
                pkg.Write(TargetPrefab[i] ?? "");
                pkg.Write(TargetCount[i]);
                pkg.Write(TargetLevel[i]);
            }

            pkg.Write(RewardsAmount);
            for (int i = 0; i < RewardsAmount; ++i)
            {
                pkg.Write((int)RewardType[i]);
                pkg.Write(RewardPrefab[i] ?? "");
                pkg.Write(RewardCount[i]);
                pkg.Write(RewardLevel[i]);
            }

            pkg.Write(Conditions.Length);
            for (int i = 0; i < Conditions.Length; ++i)
            {
                pkg.Write(Conditions[i] ?? "");
            }

            pkg.Write(PreviewImage ?? "");
            pkg.Write(Cooldown);
            pkg.Write(TimeLimit);
        }


        public void Deserialize(ref ZPackage pkg)
        {
            Type = (QuestType)pkg.ReadInt();
            SpecialTag = (SpecialQuestTag)pkg.ReadInt();
            Name = pkg.ReadString();
            Description = pkg.ReadString();
            TargetAmount = pkg.ReadInt();
            TargetPrefab = new string[TargetAmount];
            TargetCount = new int[TargetAmount];
            TargetLevel = new int[TargetAmount];
            for (int i = 0; i < TargetAmount; ++i)
            {
                TargetPrefab[i] = pkg.ReadString();
                TargetCount[i] = pkg.ReadInt();
                TargetLevel[i] = pkg.ReadInt();
            }

            RewardsAmount = pkg.ReadInt();
            RewardType = new QuestRewardType[RewardsAmount];
            RewardPrefab = new string[RewardsAmount];
            RewardCount = new int[RewardsAmount];
            RewardLevel = new int[RewardsAmount];
            for (int i = 0; i < RewardsAmount; ++i)
            {
                RewardType[i] = (QuestRewardType)pkg.ReadInt();
                RewardPrefab[i] = pkg.ReadString();
                RewardCount[i] = pkg.ReadInt();
                RewardLevel[i] = pkg.ReadInt();
            }

            int conditionsAmount = pkg.ReadInt();
            Conditions = new string[conditionsAmount];
            for (int i = 0; i < conditionsAmount; ++i)
            {
                Conditions[i] = pkg.ReadString();
            }

            PreviewImage = pkg.ReadString();
            Cooldown = pkg.ReadInt();
            TimeLimit = pkg.ReadInt();
        }
    }

    //main data part
    public partial class Quest
    {
        public QuestType Type;
        public SpecialQuestTag SpecialTag;
        public string Name;
        public string Description;
        public int TargetAmount;
        public string[] TargetPrefab;
        public int[] TargetCount;
        public int[] TargetLevel;
        public int RewardsAmount;
        public QuestRewardType[] RewardType;
        public string[] RewardPrefab;
        public int[] RewardCount;
        public int[] RewardLevel;

        public string[] Conditions;
        public Dialogues_DataTypes.Dialogue_Condition Condition;
        
        public string PreviewImage;
        public int Cooldown;
        public int TimeLimit;


        public int[] ScoreArray { get; private set; }
        public long AcceptedTime { get; private set; }
        private string[] LocalizedReward;
        private string[] LocalizedTarget;
        public Sprite GetPreviewSprite { get; private set; }
        public void SetPreviewSprite(Sprite sprite) => GetPreviewSprite = sprite;

        public override string ToString()
        {
            return "";
#pragma warning disable CS0162
            StringBuilder result = new();
            result.AppendLine($"\nQuest Name: {Name}");
            result.AppendLine($"Quest Description: {Description}");
            result.AppendLine($"Quest Type: {Type}");
            result.AppendLine($"Quest Target: {TargetPrefab}");
            result.AppendLine($"Quest Target Amount: {TargetCount}");
            result.AppendLine($"Quest Target Level: {TargetLevel}");
            result.AppendLine($"Quest Cooldown (Ingame Days): {Cooldown}");
            result.AppendLine("Quest Rewards:");
            for (int i = 0; i < RewardsAmount; ++i)
            {
                result.AppendLine(
                    $"{i + 1}) Quest Reward Type: {RewardType[i]} | Quest Reward Prefab: {RewardPrefab[i]} | Quest Reward Amount: {RewardCount[i]} | Quest Reward Level: {RewardLevel[i]}");
            }

            return result.ToString();
#pragma warning restore CS0162
        }

        public int GetScore(int index) => ScoreArray[index];

        public bool IsComplete()
        {
            for (int i = 0; i < TargetAmount; ++i)
                if (ScoreArray[i] < TargetCount[i])
                    return false;
            return true;
        }


        public bool IsComplete(int index) => ScoreArray[index] >= TargetCount[index];


        public bool Init()
        {
            bool STOP = false;
            for (int i = 0; i < RewardsAmount; ++i)
            {
                GameObject rewardPrefab = ZNetScene.instance.GetPrefab(RewardPrefab[i]);
                if (!rewardPrefab && RewardType[i] is QuestRewardType.Item or QuestRewardType.Pet)
                {
                    STOP = true;
                }
            }

            if (Type != QuestType.Talk)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    GameObject targetPrefab = ZNetScene.instance.GetPrefab(TargetPrefab[i]);
                    if (!targetPrefab)
                    {
                        STOP = true;
                        break;
                    }
                }
            }
            
            if (STOP) return false;
            Condition = Dialogues_DataTypes.Dialogue.ParseConditions(Conditions, true);
            LocalizedReward = new string[RewardsAmount];
            LocalizedTarget = new string[TargetAmount];
            for (int i = 0; i < RewardsAmount; ++i)
            {
                LocalizedReward[i] = "";
                GameObject rewardPrefab = ZNetScene.instance.GetPrefab(RewardPrefab[i]);
                switch (RewardType[i])
                {
                    case QuestRewardType.Pet:
                        if (!rewardPrefab.GetComponent<Character>()) return false;
                        PhotoManager.__instance.MakeSprite(rewardPrefab, 0.6f, 0.25f, RewardLevel[i]);
                        LocalizedReward[i] =
                            Localization.instance.Localize(rewardPrefab.GetComponent<Character>().m_name);
                        break;
                    case QuestRewardType.Item:
                        if (!rewardPrefab.GetComponent<ItemDrop>()) return false;
                        LocalizedReward[i] = Localization.instance.Localize(rewardPrefab.GetComponent<ItemDrop>()
                            .m_itemData.m_shared
                            .m_name);
                        break;
                    case QuestRewardType.Skill or QuestRewardType.Skill_EXP:
                        LocalizedReward[i] = Utils.LocalizeSkill(RewardPrefab[i]);
                        break;
                    case QuestRewardType.AddCustomValue or QuestRewardType.SetCustomValue:
                        LocalizedReward[i] = RewardPrefab[i].Replace("_", " ");
                        break;
                }
            }

            if (Type is QuestType.Talk)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    LocalizedTarget[i] = Utils.RichTextFormatting(TargetPrefab[i]);
                }

                return true;
            }


            if (Type is QuestType.Collect or QuestType.Craft)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    GameObject targetPrefab = ZNetScene.instance.GetPrefab(TargetPrefab[i]);
                    if (!targetPrefab.GetComponent<ItemDrop>()) return false;
                    LocalizedTarget[i] =
                        Localization.instance.Localize(targetPrefab.GetComponent<ItemDrop>().m_itemData.m_shared
                            .m_name);
                }

                return true;
            }

            if (Type == QuestType.Kill)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    GameObject targetPrefab = ZNetScene.instance.GetPrefab(TargetPrefab[i]);
                    if (!targetPrefab.GetComponent<Character>()) return false;
                    PhotoManager.__instance.MakeSprite(targetPrefab, 0.6f, 0.25f, TargetLevel[i]);
                    LocalizedTarget[i] = Localization.instance.Localize(targetPrefab.GetComponent<Character>().m_name);
                }

                return true;
            }

            if (Type == QuestType.Harvest)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    GameObject targetPrefab = ZNetScene.instance.GetPrefab(TargetPrefab[i]);
                    if (!targetPrefab.GetComponent<Pickable>()) return false;
                    PhotoManager.__instance.MakeSprite(targetPrefab, 0.9f, 0f);
                    LocalizedTarget[i] =
                        Localization.instance.Localize(targetPrefab.GetComponent<Pickable>().GetHoverName());
                }

                return true;
            }

            if (Type == QuestType.Build)
            {
                for (int i = 0; i < TargetAmount; ++i)
                {
                    GameObject targetPrefab = ZNetScene.instance.GetPrefab(TargetPrefab[i]);
                    if (!targetPrefab.GetComponent<Piece>()) return false;
                    LocalizedTarget[i] = Localization.instance.Localize(targetPrefab.GetComponent<Piece>().m_name);
                }

                return true;
            }

            return false;
        }

        public string GetLocalizedReward(int index) => LocalizedReward[index];
        public string GetLocalizedTarget(int index) => LocalizedTarget[index];


        public static bool CanTake(int UID, out string message, out Dialogues_DataTypes.OptionCondition type)
        {
            message = "";
            type = Dialogues_DataTypes.OptionCondition.None;
            if (!AllQuests.ContainsKey(UID) || !Player.m_localPlayer) return false;
            Quest CheckQuest = AllQuests[UID];
            if (CheckQuest.Condition == null) return true;
            foreach (Dialogues_DataTypes.Dialogue_Condition cast in CheckQuest.Condition.GetInvocationList().Cast<Dialogues_DataTypes.Dialogue_Condition>())
            {
                if (!cast(out message, out type)) return false;
            }
            return true;
        }


        public static bool IsAccepted(int UID)
        {
            return AcceptedQuests.ContainsKey(UID);
        }

        public static bool IsOnCooldown(int UID, out int left)
        {
            left = 0;
            if (!Player.m_localPlayer) return true;
            string cooldown = "[MPASN]questCD=" + UID;
            if (!Player.m_localPlayer.m_customData.ContainsKey(cooldown)) return false;
            int day = Convert.ToInt32(Player.m_localPlayer.m_customData[cooldown]);
            left = (day + AllQuests[UID].Cooldown) - EnvMan.instance.GetCurrentDay();
            return left > 0;
        }

        public static void AcceptQuest(int UID, string score = "0", string acceptedTime = null, bool handleEvent = true)
        {
            if (!Player.m_localPlayer) return;
            if (AcceptedQuests.ContainsKey(UID) || !AllQuests.ContainsKey(UID)) return;
            AcceptedQuests[UID] = AllQuests[UID];
            AcceptedQuests[UID].ScoreArray = new int[AcceptedQuests[UID].TargetAmount];

            string[] split = score.Split(',');
            for (int i = 0; i < split.Length; ++i)
            {
                if (i >= AcceptedQuests[UID].ScoreArray.Length) break;
                AcceptedQuests[UID].ScoreArray[i] = Convert.ToInt32(split[i]);
            }

            AcceptedQuests[UID].AcceptedTime = acceptedTime == null
                ? (long)EnvMan.instance.m_totalSeconds
                : Convert.ToInt64(acceptedTime);

            if (handleEvent) HandleQuestEvent(UID, QuestEventCondition.OnAcceptQuest);
            InventoryChanged();
            SaveScore(UID);
        }

        private static void SaveScore(int UID)
        {
            if (!Player.m_localPlayer || !AcceptedQuests.ContainsKey(UID)) return;
            string save = "[MPASN]quest=" + UID;
            string scoreSave = AcceptedQuests[UID].ScoreArray.Aggregate("", (current, i) => current + (i + ","))
                .TrimEnd(',');
            scoreSave += ";" + AcceptedQuests[UID].AcceptedTime;
            Player.m_localPlayer.m_customData[save] = scoreSave;
            Quests_UIs.AcceptedQuestsUI.UpdateStatus(AcceptedQuests[UID]);
        }

        public static void RemoveQuestComplete(int UID)
        {
            if (!Player.m_localPlayer) return;
            if (!AcceptedQuests.ContainsKey(UID)) return;
            Quest quest = AcceptedQuests[UID];
            if (quest.Type is QuestType.Collect)
            {
                for (int i = 0; i < quest.TargetAmount; ++i)
                {
                    GameObject prefab = ZNetScene.instance.GetPrefab(quest.TargetPrefab[i]);
                    if (Utils.CustomCountItems(prefab.name, quest.TargetLevel[i]) < quest.TargetCount[i]) return;
                }

                AcceptedQuests.Remove(UID);

                for (int i = 0; i < quest.TargetAmount; ++i)
                {
                    GameObject prefab = ZNetScene.instance.GetPrefab(quest.TargetPrefab[i]);
                    Utils.CustomRemoveItems(prefab.name, quest.TargetCount[i], quest.TargetLevel[i]);
                }
            }
            else
            {
                AcceptedQuests.Remove(UID);
            }

            for (int i = 0; i < quest.RewardsAmount; ++i)
            {
                if (quest.RewardType[i] is QuestRewardType.EpicMMO_EXP)
                {
                    EpicMMOSystem_API.AddExp(quest.RewardCount[i]);
                    continue;
                }

                if (quest.RewardType[i] is QuestRewardType.Cozyheim_EXP)
                {
                    Cozyheim_LevelingSystem.AddExp(quest.RewardCount[i]);
                    continue;
                }
                if (quest.RewardType[i] is QuestRewardType.AddCustomValue)
                {
                        Player.m_localPlayer.AddCustomValue(quest.RewardPrefab[i], quest.RewardCount[i]);
                }
                if (quest.RewardType[i] is QuestRewardType.SetCustomValue)
                {
                    if (quest.RewardCount[i] != 0)
                        Player.m_localPlayer.SetCustomValue(quest.RewardPrefab[i], quest.RewardCount[i]);
                    else
                        Player.m_localPlayer.RemoveCustomValue(quest.RewardPrefab[i]);
                }

                if (quest.RewardType[i] is QuestRewardType.MH_EXP)
                {
                    MH_API.AddEXP(quest.RewardCount[i]);
                    continue;
                }

                if (quest.RewardType[i] is QuestRewardType.GuildAddLevel)
                {
                    if (Guilds.API.GetOwnGuild() is { } g)
                    {
                        g.General.level += quest.RewardCount[i];
                        Guilds.API.SaveGuild(g);
                    }
                    continue;
                }

                if (quest.RewardType[i] is QuestRewardType.Battlepass_EXP)
                {
                    const string key = "[kg.BP]bp";
                    if(!Player.m_localPlayer.m_customData.TryGetValue(key, out var bp_data)) return;
                    string[] bpSplit = bp_data.Split('|');
                    int exp = Convert.ToInt32(bpSplit[1]);
                    exp += quest.RewardCount[i];
                    bpSplit[1] = exp.ToString();
                    Player.m_localPlayer.m_customData[key] = $"{bpSplit[0]}|{bpSplit[1]}|{bpSplit[2]}|{bpSplit[3]}";
                    continue;
                }

                if (quest.RewardType[i] is QuestRewardType.Item or QuestRewardType.Pet)
                {
                    Utils.InstantiateItem(ZNetScene.instance.GetPrefab(quest.RewardPrefab[i]),
                        quest.RewardCount[i], quest.RewardLevel[i]);
                }

                if (quest.RewardType[i] is QuestRewardType.Skill)
                {
                    if (Utils.GetPlayerSkillLevelCustom(quest.RewardPrefab[i]) >
                        (Marketplace.TempProfessionsType != null ? 0 : -1))
                        Player.m_localPlayer.GetSkills().CheatRaiseSkill(quest.RewardPrefab[i],
                            quest.RewardCount[i]);
                }

                if (quest.RewardType[i] is QuestRewardType.Skill_EXP)
                {
                    if (Utils.GetPlayerSkillLevelCustom(quest.RewardPrefab[i]) >
                        (Marketplace.TempProfessionsType != null ? 0 : -1))
                    {
                        Utils.IncreaseSkillEXP(quest.RewardPrefab[i], quest.RewardCount[i]);
                    }
                }
            }

            string save = "[MPASN]quest=" + UID;
            if (Player.m_localPlayer.m_customData.ContainsKey(save))
            {
                Player.m_localPlayer.m_customData.Remove(save);
            }

            string cooldown = "[MPASN]questCD=" + UID;
            Player.m_localPlayer.m_customData[cooldown] = EnvMan.instance.GetCurrentDay().ToString();
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                $"<color=#00ff00>$mpasn_youfinishedquest:</color> <color=#00FFFF>{AllQuests[UID].Name}</color>"
                    .Localize());
            if (ZNet.instance.GetServerPeer() != null)
            {
                ZPackage pkg = new();
                pkg.Write((int)DiscordStuff.DiscordStuff.Webhooks.Quest);
                pkg.Write(Player.m_localPlayer?.GetPlayerName() ?? "LocalPlayer");
                pkg.Write(AllQuests[UID].Name);
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "KGmarket CustomWebhooks",
                    pkg);
            }

            HandleQuestEvent(UID, QuestEventCondition.OnCompleteQuest);
        }

        public static void RemoveQuestFailed(int UID, bool handleEvent = true)
        {
            if (!Player.m_localPlayer) return;
            if (!AcceptedQuests.ContainsKey(UID)) return;
            AcceptedQuests.Remove(UID);
            string save = "[MPASN]quest=" + UID;
            if (Player.m_localPlayer.m_customData.ContainsKey(save))
            {
                Player.m_localPlayer.m_customData.Remove(save);
            }

            if (handleEvent) HandleQuestEvent(UID, QuestEventCondition.OnCancelQuest);
        }

        public static void ClickQuestButton(int UID)
        {
            if (!Player.m_localPlayer || !AllQuests.ContainsKey(UID)) return;
            AssetStorage.AUsrc.Play();
            if (IsOnCooldown(UID, out int left))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    $"<color=#00ff00>{left}</color> {Localization.instance.Localize("$mpasn_daysleft")}");
                return;
            }

            if (!IsAccepted(UID))
            {
                if (AcceptedQuests.Count >= Global_Configs.SyncedGlobalOptions.Value._maxAcceptedQuests)
                {
                    return;
                }

                if (!CanTake(UID, out string msg, out _))
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, msg);
                    Quests_UIs.QuestUI.Hide();
                    return;
                }

                AcceptQuest(UID);
                Quests_UIs.AcceptedQuestsUI.CheckQuests();
                return;
            }

            if (AcceptedQuests[UID].IsComplete())
            {
                RemoveQuestComplete(UID);
                Quests_UIs.AcceptedQuestsUI.CheckQuests();
                return;
            }

            RemoveQuestFailed(UID);
            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }

        public static void TryAddRewardKill(string prefab, int level)
        {
            HashSet<int> ToAutocomplete = new();
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Kill && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab && level >= quest.Value.TargetLevel[i])
                    {
                        quest.Value.ScoreArray[i]++;
                        SaveScore(quest.Key);
                        CheckCharacterTargets();

                        if (quest.Value.SpecialTag.HasFlagFast(SpecialQuestTag.Autocomplete) &&
                            quest.Value.IsComplete())
                        {
                            ToAutocomplete.Add(quest.Key);
                        }

                        if (!Global_Configs.SyncedGlobalOptions.Value._allowMultipleQuestScore)
                            break;
                    }
                }
            }

            if (ToAutocomplete.Count <= 0) return;
            foreach (int quest in ToAutocomplete)
            {
                RemoveQuestComplete(quest);
            }

            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }

        public static bool IsQuestTarget(string NPCName)
        {
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Talk && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == NPCName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsQuestTarget(Character c)
        {
            string prefab = global::Utils.GetPrefabName(c.gameObject);
            int level = c.GetLevel();
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Kill && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab && level >= quest.Value.TargetLevel[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsQuestTarget(Recipe c, int level)
        {
            string prefab = c.m_item.gameObject.name;
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Craft && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab && level == quest.Value.TargetLevel[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void CheckCharacterTargets()
        {
            foreach (Character c in Character.GetAllCharacters())
            {
                c.transform.Find("MPASNquest")?.gameObject.SetActive(IsQuestTarget(c));
            }
        }

        public static void CheckItemDropsTargets()
        {
            foreach (ItemDrop c in ItemDrop.s_instances)
            {
                c.transform.Find("MPASNquest")?.gameObject.SetActive(IsQuestTarget(c));
            }
        }

        public static void CheckTalkTargets()
        {
            foreach (Market_NPC.NPCcomponent c in Market_NPC.NPCcomponent.ALL)
            {
                c.transform.Find("MPASNquest")?.gameObject.SetActive(IsQuestTarget(c.GetNPCName()));
            }
        }

        public static void CheckPickableTargets()
        {
            foreach (Pickable c in Quest_ProgressionHook.Pickable_Hook.GetPickables())
            {
                if (!c) continue;
                c.transform.Find("MPASNquest")?.gameObject.SetActive(IsQuestTarget(c));
            }
        }


        public static bool IsQuestTarget(Pickable itemDrop)
        {
            string prefab = global::Utils.GetPrefabName(itemDrop.gameObject);
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Harvest && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsQuestTarget(ItemDrop itemDrop)
        {
            string prefab = global::Utils.GetPrefabName(itemDrop.gameObject);
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Collect && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab &&
                        itemDrop.m_itemData.m_quality == quest.Value.TargetLevel[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsQuestTarget(Piece piece)
        {
            string prefab = piece.gameObject.name;
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Build && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void TryAddRewardCraft(string prefab, int level)
        {
            HashSet<int> ToAutocomplete = new();
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Craft && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab && level == quest.Value.TargetLevel[i])
                    {
                        quest.Value.ScoreArray[i]++;
                        SaveScore(quest.Key);

                        if (quest.Value.SpecialTag.HasFlagFast(SpecialQuestTag.Autocomplete) &&
                            quest.Value.IsComplete())
                        {
                            ToAutocomplete.Add(quest.Key);
                        }

                        if (!Global_Configs.SyncedGlobalOptions.Value._allowMultipleQuestScore)
                            break;
                    }
                }
            }

            if (ToAutocomplete.Count <= 0) return;
            foreach (int quest in ToAutocomplete)
            {
                RemoveQuestComplete(quest);
            }

            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }

        public static bool TryAddRewardBuild(string prefab)
        {
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Build && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab)
                    {
                        quest.Value.ScoreArray[i]++;
                        SaveScore(quest.Key);

                        if (quest.Value.SpecialTag.HasFlagFast(SpecialQuestTag.Autocomplete) &&
                            quest.Value.IsComplete())
                        {
                            RemoveQuestComplete(quest.Key);
                            Quests_UIs.AcceptedQuestsUI.CheckQuests();
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryAddRewardTalk(string name)
        {
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Talk && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == name)
                    {
                        quest.Value.ScoreArray[i] = 1;
                        if (quest.Value.IsComplete())
                        {
                            RemoveQuestComplete(quest.Key);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public static void TryAddRewardPickup(string prefab)
        {
            HashSet<int> ToAutocomplete = new();
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(q =>
                         q.Value.Type == QuestType.Harvest && !q.Value.IsComplete()))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    if (quest.Value.IsComplete(i)) continue;
                    if (quest.Value.TargetPrefab[i] == prefab)
                    {
                        quest.Value.ScoreArray[i]++;
                        SaveScore(quest.Key);
                        CheckPickableTargets();

                        if (quest.Value.SpecialTag.HasFlagFast(SpecialQuestTag.Autocomplete) &&
                            quest.Value.IsComplete())
                        {
                            ToAutocomplete.Add(quest.Key);
                        }

                        if (!Global_Configs.SyncedGlobalOptions.Value._allowMultipleQuestScore)
                            break;
                    }
                }
            }

            if (ToAutocomplete.Count <= 0) return;
            foreach (int quest in ToAutocomplete)
            {
                RemoveQuestComplete(quest);
            }

            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }


        public static void InventoryChanged()
        {
            HashSet<int> ToAutocomplete = new();
            foreach (KeyValuePair<int, Quest> quest in AcceptedQuests.Where(t => t.Value.Type == QuestType.Collect))
            {
                for (int i = 0; i < quest.Value.TargetAmount; ++i)
                {
                    quest.Value.ScoreArray[i] =
                        Utils.CustomCountItems(quest.Value.TargetPrefab[i], quest.Value.TargetLevel[i]);
                    SaveScore(quest.Key);
                    CheckItemDropsTargets();

                    if (quest.Value.SpecialTag.HasFlagFast(SpecialQuestTag.Autocomplete) && quest.Value.IsComplete())
                    {
                        ToAutocomplete.Add(quest.Key);
                    }
                }
            }

            if (ToAutocomplete.Count <= 0) return;
            foreach (int quest in ToAutocomplete)
            {
                RemoveQuestComplete(quest);
            }

            Quests_UIs.AcceptedQuestsUI.CheckQuests();
        }
    }

    private static void HandleQuestEvent(int UID, QuestEventCondition type)
    {
        if (!SyncedQuestsEvents.Value.TryGetValue(UID, out List<QuestEvent> events)) return;
        IEnumerable<QuestEvent> search = events.Where(x => x.cond == type);
        if (!search.Any()) return;
        foreach (QuestEvent quest in search)
        { 
            QuestEventAction action = quest.action;
            string[] split = quest.args.Split(',');
            try
            {
                switch (action)
                {
                    case QuestEventAction.GiveItem:
                        string itemPrefab = split[0];
                        GameObject prefab = ZNetScene.instance.GetPrefab(itemPrefab);
                        if (!prefab || !prefab.GetComponent<ItemDrop>()) continue;
                        int amount = int.Parse(split[1]);
                        int level = int.Parse(split[2]);
                        Utils.InstantiateItem(prefab, amount, level);
                        break;
                    case QuestEventAction.GiveQuest:
                        string questName = split[0].ToLower();
                        Quest.AcceptQuest(questName.GetStableHashCode(), handleEvent: false);
                        Quests_UIs.AcceptedQuestsUI.CheckQuests();
                        break;
                    case QuestEventAction.Spawn:
                        string spawnPrefab = split[0];
                        Vector3 spawnPos = Player.m_localPlayer.transform.position;
                        GameObject spawn = ZNetScene.instance.GetPrefab(spawnPrefab);
                        if (!spawn || !spawn.GetComponent<Character>()) continue;
                        int spawnAmount = int.Parse(split[1]);
                        int spawnLevel = Mathf.Max(1, int.Parse(split[2]) + 1);
                        for (int i = 0; i < spawnAmount; ++i)
                        {
                            float randomX = UnityEngine.Random.Range(-15, 15);
                            float randomZ = UnityEngine.Random.Range(-15, 15);
                            Vector3 randomPos = new Vector3(spawnPos.x + randomX, spawnPos.y, spawnPos.z + randomZ);
                            float height = ZoneSystem.instance.GetSolidHeight(randomPos);
                            randomPos.y = height;
                            GameObject newSpawn = UnityEngine.Object.Instantiate(spawn, randomPos,
                                Quaternion.identity);
                            newSpawn.GetComponent<Character>().SetLevel(spawnLevel);
                        }

                        Quests_UIs.QuestUI.Hide();
                        break;
                    case QuestEventAction.Teleport:
                        int x = int.Parse(split[0]);
                        int y = int.Parse(split[1]);
                        int z = int.Parse(split[2]);
                        Player.m_localPlayer.TeleportTo(new Vector3(x, y, z), Player.m_localPlayer.transform.rotation,
                            true);
                        Quests_UIs.QuestUI.Hide();
                        break;
                    case QuestEventAction.Damage:
                        int damage = int.Parse(split[0]);
                        HitData hitData = new HitData();
                        hitData.m_damage.m_damage = damage;
                        Player.m_localPlayer.Damage(hitData);
                        break;
                    case QuestEventAction.Heal:
                        int heal = int.Parse(split[0]);
                        Player.m_localPlayer.Heal(heal);
                        break;
                    case QuestEventAction.RemoveQuest:
                        string removeQuestName = split[0].ToLower();
                        Quest.RemoveQuestFailed(removeQuestName.GetStableHashCode(), false);
                        Quests_UIs.AcceptedQuestsUI.CheckQuests();
                        break;
                    case QuestEventAction.PlaySound:
                        string sound = split[0];
                        if (AssetStorage.NPC_AudioClips.TryGetValue(sound, out AudioClip clip))
                            AssetStorage.AUsrc.PlayOneShot(clip);
                        break;
                    case QuestEventAction.NpcText:
                        string text = quest.args;
                        GameObject closestNPC = Utils.GetClosestNPC(Player.m_localPlayer.transform.position).gameObject;
                        if (closestNPC == null) closestNPC = Player.m_localPlayer.gameObject;
                        Chat.instance.SetNpcText(closestNPC, Vector3.up * 4f, 20f, 7f, "", text, false);
                        break;
                    default:
                        continue;
                }
            }
            catch
            {
                // 
            }
        }
    }
}