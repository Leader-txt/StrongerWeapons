using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;

namespace StrongerWeapons
{
    static class Utils
    {
        public static NPC GetNearByNPC(int who, bool friendly = false)
        {
            var plr = TShock.Players[who];
            float min = float.MaxValue;
            NPC result = null;
            foreach (var npc in Main.npc)
            {
                if (npc.friendly == friendly)
                {
                    var dis = Vector2.Distance(npc.position, plr.TPlayer.position);
                    if (dis < min)
                    {
                        min = dis;
                        result = npc;
                    }
                }
            }
            return result;
        }
        public static double Mod(Vector2 vec)
        {
            return Math.Sqrt(Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2));
        }
        /// <summary>
        /// 概率生成器
        /// </summary>
        /// <param name="Rate">概率</param>
        /// <returns>触发</returns>
        public static bool Rate(int Rate)
        {
            return StrongerWeapons.random.Next(0, 101) <= Rate;
        }
        /// <summary>
        /// 获取在线人数
        /// </summary>
        /// <returns>在线人数</returns>
        public static int GetOnlineCount()
        {
            int i = 0;
            foreach (var p in TShock.Players)
            {
                if (p != null && p.Active)
                    i++;
            }
            return i;
        }
        /// <summary>
        /// 学习技能
        /// </summary>
        /// <param name="name">玩家名</param>
        /// <param name="index">技能索引</param>
        public static void LearnSkill(string name,int index,int weapon)
        {
            var config = Config.GetConfig();
            var skill = config.Skills[index];
            var player = TSPlayer.FindByNameOrID(name)[0];
            if (!player.HasPermission(skill.Permission))
            {
                player.SendErrorMessage("您无权学习此技能！");
                return;
            }
            try
            {
                var list = StrongerWeapons.Learnts[player.Index];
                if (list.FindAll((Learnt l) => l.Skill.NetID==skill.NetID && l.NetID ==weapon).Count != 0)//如果已学习此技能
                {
                    player.SendInfoMessage("您已学习过此技能，无需重复学习");
                    return;
                }
            }
            catch { }
            if(!skill.Has (player.Index ))
            {
                string drops = "";
                foreach (var d in skill.Drop)
                {
                    drops += $"\n击杀{Lang.GetNPCNameValue(d.BossID)}有{d.Rate}%的概率掉落此技能书";
                }
                player.SendErrorMessage($"背包中不存在指定技能书"+drops);
                return;
            }
            var Weapon = Weapons.GetWeapon(player.Name, weapon);
            if (skill.Level > 0)
            {
                if (Weapon == null || Weapon.Level < skill.Level)
                {
                    player.SendErrorMessage("武器未达到指定等级，无法学习此技能！");
                    return;
                }
            }
            if (Weapon != null)
            {
                Weapon.Damage += skill.Damage;
                Weapon.UseTime -= skill.UseTime;
                Weapon.Save();
                UpdateWea(player.Index, Weapon);
            }
            var learn = new Learnt { Name = name, Index = index, NetID = weapon };
            learn.Save();
            try
            {
                StrongerWeapons.Learnts[player.Index].Add(learn);
            }
            catch
            {
                StrongerWeapons.Learnts.Add(player.Index, new List<Learnt>());
                StrongerWeapons.Learnts[player.Index].Add(learn);
            }
            Utils.DelItem(player.Index, new Item { netID = skill.NetID, Perfix = Skill.Prefix }, true);
            player.SendSuccessMessage("已习得技能:" + skill.Name);
        }
        /// <summary>
        /// 给予强化的物品
        /// </summary>
        /// <param name="weapons">武器</param>
        public static void GiveCItem(Weapons weapons)
        {
            var player = TSPlayer.FindByNameOrID(weapons.Name)[0];
            var item = TShock.Utils.GetItemById(weapons.NetID);
            int itemIndex = Terraria.Item.NewItem(null,(int)player.X, (int)player.Y, item.width, item.height, item.type, item.maxStack);
            Terraria.Item targetItem = Main.item[itemIndex];
            targetItem.playerIndexTheItemIsReservedFor = player.Index;
            targetItem.damage *= (weapons.Damage+100);
            targetItem.damage /= 100;
            targetItem.knockBack *= (weapons.Knockback + 100);
            targetItem.knockBack /= 100;
            targetItem.useTime *= (100 - weapons.UseTime);
            targetItem.useTime /= 100;
            targetItem.useAnimation *= 100 - weapons.UseTime;
            targetItem.useAnimation /= 100;
            if (targetItem.useAnimation < 2)
            {
                targetItem.useAnimation = 2;
            }
            targetItem.scale *= (weapons.Size + 100);
            targetItem.scale /= 100;

            TSPlayer.All.SendData(PacketTypes.UpdateItemDrop, null, itemIndex);
            TSPlayer.All.SendData(PacketTypes.ItemOwner, null, itemIndex);
            TSPlayer.All.SendData(PacketTypes.TweakItem, null, itemIndex, 255, 63);
        }
        /// <summary>
        /// 更新玩家背包
        /// </summary>
        /// <param name="who">玩家index</param>
        public static void UpdateInv(int who)
        {
            var player = TShock.Players[who];
            for (int i = 0; i < 58; i++)
                player.SendData(PacketTypes.PlayerSlot, "", player.Index, i, player.TPlayer.inventory[i].prefix);
        }
        /// <summary>
        /// 更新玩家指定背包索引格
        /// </summary>
        /// <param name="who">玩家</param>
        /// <param name="index">背包物品索引</param>
        public static void UpdateInv(int who,int index)
        {
            var player = TShock.Players[who];
            player.SendData(PacketTypes.PlayerSlot, "", player.Index, index, player.TPlayer.inventory[index].prefix);
        }
        /// <summary>
        /// 强化玩家背包中的指定武器
        /// </summary>
        /// <param name="who">玩家索引</param>
        /// <param name="weapons">强化武器对象</param>
        public static void UpdateWea(int who ,Weapons weapons)
        {
            var player = TShock.Players[who];
            for (int i = 0; i < 58; i++)
            {
                if(player.TPlayer.inventory[i].netID==weapons.NetID)
                {
                    player.TPlayer.inventory[i].stack = 0;
                    UpdateInv(who, i);
                    GiveCItem(weapons);
                }
            }
        }
        /// <summary>
        /// 强化玩家背包中的所有武器
        /// </summary>
        /// <param name="who">玩家索引</param>
        public static void UpdateWeas(int who)
        {
            foreach (var w in Weapons.GetWeapons(TShock.Players[who].Name))
                UpdateWea(who, w);
        }
        public static bool CanDel(int who ,Item item,bool CheckPrefix=false)
        {
            var player = TShock.Players[who];
            int stack = 0;
            for (int i = 0; i < 58; i++)
            {
                if (player.TPlayer.inventory[i].netID == item.netID)
                {
                    if (CheckPrefix && player.TPlayer.inventory[i].prefix != item.Perfix)
                        continue;
                    stack += player.TPlayer.inventory[i].stack;
                }
            }
            if (stack >= item.stack)
                return true;
            else
                return false;
        }
        public static bool CanDel(int who,Item[] items)
        {
            foreach (var i in items)
            {
                if (!CanDel(who, i))
                    return false;
            }
            return true;
        }
        public static void DelItem(int who, Item item, bool CheckPrefix = false)
        {
            var player = TShock.Players[who];
            int stack = item.stack;
            for (int i = 0; i < 58; i++)
            {
                if (player.TPlayer.inventory[i].netID == item.netID)
                {
                    if (CheckPrefix && player.TPlayer.inventory[i].prefix != item.Perfix)
                        continue;
                    if (player.TPlayer.inventory[i].stack >= stack)
                    {
                        player.TPlayer.inventory[i].stack -= stack;
                        stack = 0;
                    }
                    else
                    {
                        stack -= player.TPlayer.inventory[i].stack;
                        player.TPlayer.inventory[i].stack = 0;
                    }
                    UpdateInv(who, i);
                }
            }
            //UpdateInv(who);
        }
        public static bool DelNeeds(int who ,Item[] items)
        {
            if(!CanDel(who, items))
            {
                return false;
            }
            foreach (var i in items)
            {
                DelItem(who, i);
            }
            return true;
        }
        /// <summary>
        /// 是否可以强化
        /// </summary>
        /// <param name="probability">概率</param>
        /// <returns></returns>
        public static bool CanStr(int probability)
        {
            return StrongerWeapons.random.Next(1, 100) <= probability ? true : false;
        }
        public static void StrWea(int who,Stronger stronger)
        {
            new Task(() =>
            {
                var player = TShock.Players[who];
                while (true)
                {
                    bool str = true;
                    foreach(var p in TShock.Players)
                    {
                        if (p == null || !p.Active)
                            continue;
                        if(p.Index != who)
                        {
                            var len = Math.Sqrt(Math.Pow(player.TileX - p.TileX, 2) + Math.Pow(player.TileY - p.TileY, 2));
                            if(len <= 5)
                            {
                                str = false;
                                break;
                            }
                        }
                    }
                    if (str)
                        break;
                    else
                    {
                        Thread.Sleep(1000);
                        player.SendErrorMessage("您附近五格半径内有其他玩家，不可强化！");
                    }
                }
                var user = User.GetUser(who, stronger.Index);
                if (user != null && user.Stack >= stronger.Stack)
                {
                    player.SendErrorMessage("强化次数已用尽，不可强化！");
                    return;
                }
                if (!DelNeeds(who, stronger.Needs))
                {
                    player.SendErrorMessage("背包中材料不足，请确认是否已将所有材料置于背包中！");
                    return;
                }
                if (CanStr(stronger.Probability))
                {
                    var wea = Weapons.GetWeapon(player.Name, stronger.NetID);
                    if (wea == null)
                    {
                        wea = new Weapons(player.Name, stronger.NetID, stronger.Damage, stronger.Knockback, stronger.UseTime, stronger.Size, 1);
                    }
                    else
                    {
                        wea.Damage += stronger.Damage;
                        wea.Knockback += stronger.Knockback;
                        wea.UseTime += stronger.UseTime;
                        wea.Size += stronger.Size;
                        wea.Level++;
                    }
                    if (wea.UseTime > 100)
                    {
                        wea.UseTime = 100;
                    }
                    wea.Save();
                    UpdateWea(who, wea);
                    if (user == null)
                    {
                        user = new User();
                        user.Name = player.Name;
                        user.id = stronger.Index;
                        user.Stack = 1;
                        user.Save();
                    }
                    else
                    {
                        user.Stack++;
                        user.Save();
                    }
                    player.SendSuccessMessage($"[i:{stronger.NetID}]{Lang.GetItemNameValue(stronger.NetID)}强化成功！\r伤害:+{stronger.Damage}%,击退:+{stronger.Knockback}%,CD:-{stronger.UseTime}%,大小:+{stronger.Size}%\r当前属性：伤害:+{wea.Damage }%,击退:+{wea.Knockback}%,CD:-{wea.UseTime}%,大小:+{wea.Size}%,等级:{wea.Level},剩余强化次数:{stronger.Stack - user.Stack}");
                }
                else
                {
                    player.SendErrorMessage($"[i:{stronger.NetID}]{Lang.GetItemNameValue(stronger.NetID)}强化失败！");
                }
            }).Start();
        }
    }
}
