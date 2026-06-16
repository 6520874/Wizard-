namespace WitcherGame
{
    // 中文说明：集中管理除剧情数据文件之外的固定界面、战斗和系统文案。
    public static class GameText
    {
        public const string HunterName = "猎魔人";
        public const string TrissName = "特莉丝";
        public const string YenneferName = "叶奈法";
        public const string MonsterFallbackName = "怪物";

        public static class Common
        {
            public const string Empty = "空";
            public const string None = "无";
            public const string Close = "关闭";
            public const string Back = "返回";
            public const string Leave = "离开";
            public const string Standby = "待命";
            public const string Joined = "入队";
            public const string InParty = "已上阵";
            public const string Locked = " 锁定";
            public const string ActiveLocked = " 上阵锁定";
            public const string Fixed = " 固定";
            public const string NotAvailable = "暂无";
        }

        public static class Stats
        {
            public const string MpLabel = "MP";
            public const string Attack = "攻击";
            public const string Defense = "防御";
            public const string Magic = "魔力";
            public const string Speed = "速度";
            public const string Critical = "暴击";
            public const string Experience = "经验";
            public const string Gold = "金币";
            public static string Hp(int current, int max) => $"HP {current} / {max}";
            public static string Mp(int current, int max) => $"MP {current} / {max}";
            public static string Sp(int current, int max) => $"SP {current} / {max}";
            public static string CompactHp(int current, int max) => $"HP {current}/{max}";
            public static string CompactMp(int current, int max) => $"MP {current}/{max}";
            public static string GoldAmount(int amount) => $"金币 {amount}";
            public static string GoldGain(int amount) => $"+{amount} 金币";
            public static string ExperienceGain(int amount) => $"+{amount} XP";
        }

        public static class Opening
        {
            public const string ContinueHint = "点击或按空格继续";
            public static string MissingMusic(string path) => $"Opening music clip not found at Resources/{path}";
        }

        public static class Dialogue
        {
            public const string SpeakerPlaceholder = "角色名";
            public const string ContentPlaceholder = "对白";
            public const string ContinueHint = "Enter 继续";
            public const string CurrentTaskPlaceholder = "当前任务";
            public static string CurrentTask(string objective) => $"当前任务：{objective}";
            public static string CompletedTask(string objective) => $"任务完成：{objective}";
        }

        public static class Quest
        {
            public const string TitlePlaceholder = "任务标题";
            public const string DescriptionPlaceholder = "任务描述";
            public const string ObjectivesPlaceholder = "任务目标";
            public const string TrackHint = "点击任务面板：前往当前目标";
            public const string NoTrackTarget = "当前目标暂无可追踪地点";
            public const string MovingToTarget = "正在前往当前目标";
            public const string ArrivedNearTarget = "已经到达目标附近";
            public const string CompletedPrefix = "✓ ";
            public const string PendingPrefix = "□ ";
        }

        public static class Menu
        {
            public const string CommandTitle = "猎魔命令";
            public const string Status = "状态";
            public const string Item = "道具";
            public const string Skill = "技能";
            public const string Party = "队伍";
            public const string Talk = "对话";
            public const string System = "系统";
            public const string StatusDescription = "查看队伍生命、魔力、基础属性与当前任务。";
            public const string ItemDescription = "背包系统预留：药剂、委托物品和战利品将从这里使用。";
            public const string SkillDescription = "查看猎魔法印、女术士法术、血魔法和怪物伙伴技能。";
            public const string PartyDescription = "查看当前队伍成员与怪物伙伴预留位。";
            public const string TalkDescription = "与队友闲聊，获取当前区域的线索。";
            public const string SystemDescription = "系统设置预留：存档、读档、选项和返回标题。";
            public const string CloseDescription = "关闭命令菜单，返回探索。";
            public const string ItemPlaceholder = "道具袋里只有几瓶药剂和怪物牙。真实背包接口已预留。";
            public const string SystemPlaceholder = "系统菜单预留中。之后可接入存档、音量和按键设置。";
            public const string NoActiveQuest = "暂无进行中的委托。";
            public const string CurrentTaskHeader = "当前任务";
            public const string PartyStatusHeader = "队伍状态";
            public const string SkillsHeader = "技能";
            public const string PartyManageHeader = "队伍管理";
            public const string HunterOnlyPartyRule = "当前队伍规则：只有猎魔人一个人上阵；其他角色保留为剧情成员，后续版本再开放参战。";
            public const string StoryAlliesPartyRule = "当前队伍规则：剧情盟友可上阵；未解锁成员仍保留为待命。";
        }

        public static class Hud
        {
            public const string DefaultRoom = "霜林边境";
            public const string BossName = "狂猎统领";
            public const string DefeatTitle = "你失败了";
            public const string DefeatSubtitle = "猎魔人的道路还没有结束";
            public const string Retry = "再来一次";
        }

        public static class Battle
        {
            public const string EncounterTitle = "遭遇战";
            public const string EncounterFallback = "黑暗中的怪物逼近";
            public const string Action = "行动";
            public const string Attack = "攻击";
            public const string Skill = "技能";
            public const string Item = "道具";
            public const string Defend = "防御";
            public const string Escape = "撤离战场";
            public const string Expand = "展开";
            public const string Potion = "药剂";
            public const string Guard = "护身";
            public const string Normal = "普通";
            public const string SelectAction = "选择行动。";
            public const string CharacterSkills = "角色技能";
            public const string BackHint = "Esc 返回";
            public const string NoCost = "无消耗";
            public const string Defeated = "已击败";
            public const string BattleVictory = "战斗胜利";
            public const string BattleFailure = "战斗失败……";
            public const string RoundLabel = "回合";
            public const string LootNone = "战利品：无";
            public const string LootPrefix = "战利品：";
            public const string Weakness = "弱点";
            public const string Question = "？";
            public const string ComboSlashShort = "三连银剑";
            public const string GroupFlameShort = "群体火焰";
            public const string DoubleLightningShort = "双段闪电";
            public const string AttackUpShort = "攻击提升";
            public const string SingleFlameShort = "火焰单体";
            public const string GroupDebuffShort = "群体削弱";
            public const string FrontShieldShort = "前排护盾";
            public const string FireGroupShort = "火焰群体";
            public const string ArcaneSingleShort = "奥术单体";
            public const string ArcaneGroupShort = "奥术群体";
            public const string GroupTargetShort = "群体";
            public const string SingleTargetShort = "单体";
            public const string NotEnoughManaShort = "魔力不足。";
            public const string PotionEmpty = "药剂已经用完了。";
            public const string PotionNotNeeded = "现在还不需要喝药。";
            public const string EscapeSuccess = "猎魔人撤出战斗，重新寻找机会。";
            public const string EscapeFailed = "撤退失败，怪物逼了上来！";

            public static string Encounter(string title) => $"遭遇 {title}！";
            public static string NotEnoughMana(string skillName) => $"魔力不足，无法释放{skillName}。";
            public static string SkillSelect(string actorName) => $"{actorName}：选择技能。";
            public static string PotionCount(int count) => $"药剂 x{count}";
            public static string EnemyHp(int current, int max) => $"HP {current}/{max}";
            public static string TurnPrompt(string unitName) => $"{unitName} 回合：选择攻击、技能、道具或防御。";
            public static string EnemyUseSkill(string enemyName, string skillName) => $"{enemyName} 使用 {skillName}！";
            public static string VictoryMessage(int experience, int gold) => $"战斗胜利！获得 {experience} 点经验、{gold} 枚金币。";
            public static string VictoryReward(int experience, int gold, string lootLine) => $"战斗胜利\n经验 +{experience}    金币 +{gold}\n{lootLine}";
            public static string TargetDefeated(string targetName) => $"{targetName} 被击倒了。";
            public static string StatusDotDamage(string statusName, int damage) => $"{statusName} 侵蚀猎魔人，造成 {damage} 点伤害。";
            public static string EnemyDrainLife(string enemyName, int healed) => $"{enemyName} 吸回 {healed} 点生命。";
            public static string SkillCost(int manaCost) => manaCost > 0 ? $"MP {manaCost}" : NoCost;
        }
    }
}
