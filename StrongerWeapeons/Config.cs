using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using TShockAPI;
using Newtonsoft.Json.Linq;

namespace StrongerWeapons
{
    public class Skill
    {
        public bool Has(int index)
        {
            return Utils.CanDel(index, new Item { netID = NetID, Perfix = Prefix }, true);
        }
        public void DropMe(int index,bool CheckRate=true)
        {
            var player = TShockAPI.TShock.Players[index];
            player.GiveItem(NetID, 1, Prefix);
            player.SendSuccessMessage("恭喜您获得:" + Info());
        }
        public string Info()
        {
            string info = "";
            foreach (var i in WeaponID)
            {
                info += $"[i:{i}]{Lang.GetItemNameValue(i)}";
            }
            return $"{Name},适配武器:" + info + (Level > 0 ? "等级限制:" + Level : "") + $"每{Delay }s回复{Life}+{Rate}x(在线人数-1)" +
                $"点生命和{Magic }点魔法,攻击力增加{Damage }点,攻速加快{UseTime }点,当生命低于{LowLife }%后有{CriticalRate}%" +
                $"的概率伤害倍增{Critical}倍和{DodgeRate}%的概率闪避";
        }
        public const int Prefix = 254;
        /// <summary>
        /// 学习权限
        /// </summary>
        public string Permission { get; set; } = "";
        /// <summary>
        /// 技能书名称
        /// </summary>
        public string Name { get; set; } = "技能书";
        /// <summary>
        /// 技能书适配的武器id
        /// </summary>
        public int[] WeaponID { get; set; } = new int[] { 0 };
        /// <summary>
        /// 技能书物品id
        /// </summary>
        public int NetID { get; set; } = 0;
        /// <summary>
        /// 回血值
        /// </summary>
        public int Life { get; set; } = 0;
        /// <summary>
        /// 按人数回血
        /// </summary>
        public int Rate { get; set; } = 0;//回血=Life+Rate*(在线人数-1)
        /// <summary>
        /// 回蓝
        /// </summary>
        public int Magic { get; set; } = 0;
        /// <summary>
        /// 冷却时间
        /// </summary>
        public int Delay { get; set; } = 0;
        /// <summary>
        /// 伤害附赠
        /// </summary>
        public int Damage { get; set; } = 0;
        /// <summary>
        /// 减少的攻速
        /// </summary>
        public int UseTime { get; set; } = 0;
        /// <summary>
        /// 低血量百分比
        /// </summary>
        public int LowLife { get; set; } = 0;
        /// <summary>
        /// 暴击概率
        /// </summary>
        public int CriticalRate { get; set; } = 100;
        /// <summary>
        /// 暴击伤害倍数
        /// </summary>
        public int Critical { get; set; } = 0;
        /// <summary>
        /// 闪避概率
        /// </summary>
        public int DodgeRate { get; set; } = 100;
        /// <summary>
        /// 学习等级限制
        /// </summary>
        public int Level { get; set; } = 0;
        /// <summary>
        /// 掉落boss
        /// </summary>
        public Drop[] Drop { get; set; } = new Drop[] { new Drop() };
        /// <summary>
        /// 弹幕
        /// </summary>
        public Projectile[] Projs { get; set; } = new Projectile[] { new Projectile() };
        /// <summary>
        /// 购买所需
        /// </summary>
        public Item[] Coins { get; set; } = new Item[] { new Item() };
    }
    public class Drop
    {
        /// <summary>
        /// 指定bossID
        /// </summary>
        public int BossID { get; set; } = 0;
        /// <summary>
        /// 掉落概率
        /// </summary>
        public int Rate { get; set; } = 0;
    }
    class Config
    {
        public bool FlashRightNow { get; set; } = true;
        public int FlashTimer { get; set; } = 300;
        public Skill[] Skills { get; set; } = new Skill[1] { new Skill() };

        private const string path = "tshock/StrongerWeapons.json";
        public void Save()
        {
            using (StreamWriter wr = new StreamWriter(path))
            {
                wr.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
        public static Config GetConfig()
        {
            var config = new Config();
            if(!File.Exists(path))
            {
                config.Save();
            }
            else
            {
                using (StreamReader re=new StreamReader (path))
                {
                    config = JsonConvert.DeserializeObject<Config>(re.ReadToEnd());
                }
            }
            return config;
        }
    }
}
