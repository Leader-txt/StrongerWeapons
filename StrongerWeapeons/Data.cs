using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace StrongerWeapons
{
    /// <summary>
    /// 数据
    /// 表：Weapons,Stronger
    /// Weapons:{Account(int32),ItemID(int32),Damage(int32),KnockBack(int32),UseTime(int32),size(int32),Strongered(text)}
    /// Stronger:{index(int32),ItemID(int32),Damage(int32),KnockBack(int32),UseTime(int32),size(int32)}
    /// </summary>
    static class Data
    {
        /// <summary>
        /// 数据库连接句柄
        /// </summary>
        public static System.Data.IDbConnection DB { get; set; }
        /// <summary>
        /// 连接数据库
        /// </summary>
        public static void Connect()
        {
            if (TShock.Config.Settings.StorageType == "sqlite")
            {
                string str = Path.Combine(TShock.SavePath, "StrongerWeapeons.sqlite");
                /*if (!File.Exists(str))
                {
                    SqliteConnection.CreateFile(str);
                }*/
                DB = new SqliteConnection("Data Source=" + str);
                DB.Open();
            }
        }
        private static QueryResult Result { get; set; }
        /// <summary>
        /// 执行sql命令
        /// </summary>
        /// <param name="cmd">sql命令</param>
        /// <returns>sql读取句柄</returns>
        public static IDataReader Command(string cmd)
        {
            if (TShock.Config.Settings.StorageType == "sqlite")
            {
                SqliteCommand command = new SqliteCommand(cmd, (SqliteConnection)DB);
                return command.ExecuteReader();
            }
            else
            {
                try
                {
                    Result.Dispose();
                }
                catch { }
                Result = TShock.DB.QueryReader(cmd);
                return Result.Reader;
            }
        }
        /// <summary>
        /// 初始化数据库
        /// </summary>
        public static void Init()
        {
            Connect();
            Command("create table if not exists Learnt(Name text,skill int(32),NetID int(32))");
            Command("create table if not exists User(Name text,id int(32),Stack int(32))");
            Command("create table if not exists Weapons(Name text,NetID int(32),Damage int(32),KnockBack int(32),UseTime int(32),Size int(32),Level int(32))");
            Command("create table if not exists Stronger(NetID int(32),Damage int(32),KnockBack int(32),UseTime int(32),Size int(32),Probability int(32),Stack int(32),id int(32),Needs longtext)");
        }
    }
    public class Learnt
    {
        public string Name { get; set; } = "";
        public int Index { get; set; } = 0;
        public int NetID { get; set; } = 0;
        public Skill Skill
        {
            get
            {
                var config = Config.GetConfig();
                return config.Skills[Index];
            }
        }
        public void Remove()
        {
            if(HasLearnt())
            {
                Data.Command($"delete from Learnt where Name='{Name}' and skill={Index} and NetID={NetID}");
            }
        }
        public void Save()
        {
            if(!HasLearnt())
            {
                Data.Command($"insert into Learnt(Name,skill,NetID)values('{Name}',{Index},{NetID})");
            }
        }
        public bool HasLearnt()
        {
            var reader = Data.Command($"select * from Learnt where Name='{Name}' and skill={Index} and NetID={NetID}");
            return reader.Read();
        }
        public static Learnt[] GetLearnts(string name)
        {
            var reader = Data.Command($"select skill,NetID from Learnt where Name='{name}'");
            var result = new List<Learnt>();
            while (reader.Read())
            {
                var learnt = new Learnt() { Name = name, Index = reader.GetInt32(0),NetID=reader.GetInt32(1) };
                result.Add(learnt);
            }
            return result.ToArray();
        }
    }
    public class Projectile
    {
        //弹幕id，伤害，速度
        public bool Trace { get; set; } = false;
        public int ProjID { get; set; } = 0;
        public int Speed { get; set; } = 0;
        public int Damage { get; set; } = 0;
        public float KnockBack { get; set; } = 0;
        /// <summary>
        /// 弹幕生成冷却时间
        /// </summary>
        public int Delay { get; set; } = 0;
        public bool Defined { get; set; } = false;
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float VX { get; set; } = 10;
        public float VY { get; set; } = 10;
    }
    public class Item
    {
        public override string ToString()
        {
            return $"[i/s{stack }/p{Perfix}:{netID}]{Lang.GetItemNameValue(netID)}x{stack }";
        }
        public int netID { get; set; } = 0;
        public int stack { get; set; } = 1;
        public int Perfix { get; set; } = 0;
    }
    class User
    {
        public string Name { get; set; } = "";
        public int id { get; set; } = 0;
        public int Stack { get; set; } = 0;
        public void Save()
        {
            if(GetUser(Name, id) != null)
            {
                Data.Command($"update User set Stack={Stack} where Name='{Name}' and id={id}");
            }
            else
            {
                Data.Command($"insert into User(Name,id,Stack)values('{Name}',{id},{Stack})");
            }
        }
        public static User GetUser(int who,int id)
        {
            return GetUser(TShock.Players[who].Name, id);
        }
        public static int GetStack(int who,int id)
        {
            return GetStack(TShock.Players[who].Name, id);
        }
        public static int GetStack(string Name,int id)
        {
            return GetUser(Name, id).Stack;
        }
        public static User GetUser(string Name,int id)
        {
            var reader = Data.Command($"select Stack from User where Name='{Name}' and id ={id}");
            if (reader.Read())
            {
                var user = new User();
                user.Name = Name;
                user.id = id;
                user.Stack = reader.GetInt32(0);
                return user;
            }
            else
            {
                return null;
            }
        }
    }
    class Stronger
    {
        /// <summary>
        /// 武器ID
        /// </summary>
        public int NetID { get; set; }
        /// <summary>
        /// 增加的伤害百分比
        /// </summary>
        public int Damage { get; set; }
        /// <summary>
        /// 增加的击退百分比
        /// </summary>
        public int Knockback { get; set; }
        /// <summary>
        /// 减少的CD百分比
        /// </summary>
        public int UseTime { get; set; }
        /// <summary>
        /// 增加的尺寸百分比
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        /// 强化成功概率
        /// </summary>
        public int Probability { get; set; }
        /// <summary>
        /// 最大强化次数
        /// </summary>
        public int Stack { get; set; }
        /// <summary>
        /// 该项强化索引
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 强化所需材料
        /// </summary>
        public Item[] Needs { get; set; }
        /// <summary>
        /// 新建一项强化
        /// </summary>
        /// <param name="netID">武器id</param>
        /// <param name="damage">增加的伤害百分比</param>
        /// <param name="knockback">增加的击退百分比</param>
        /// <param name="useTime">减少的CD百分比</param>
        /// <param name="size">增加的尺寸百分比</param>
        /// <param name="probablity">强化成功概率</param>
        /// <param name="stack">强化次数</param>
        /// <param name="index">该项强化索引</param>
        /// <param name="needs">强化所需材料</param>
        public Stronger(int netID,int damage,int knockback,int useTime,int size,int probablity,int stack,int index,Item[] needs)
        {
            NetID = netID;
            Damage = damage;
            Knockback = knockback;
            UseTime = useTime;
            Size = size;
            Probability = probablity;
            Stack = stack;
            Index = index;
            Needs = needs;
        }
        /// <summary>
        /// 获取所有强化
        /// </summary>
        /// <returns>强化数组</returns>
        public static Stronger[] GetStronger()
        {
            var strs = new List<Stronger>();
            var reader = Data.Command($"select * from Stronger");
            while (reader.Read())
            {
                strs.Add(new Stronger(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),reader.GetInt32(7),JsonConvert.DeserializeObject<Item[]>(reader.GetString(8))));
            }
            return strs.ToArray();
        }
        /// <summary>
        /// 获取所有关于指定武器id的强化数组
        /// </summary>
        /// <param name="NetID">武器id</param>
        /// <returns>所有关于该武器id的强化数组</returns>
        public static Stronger[] GetStrongerByID(int NetID)
        {
            var strs = new List<Stronger>();
            var reader = Data.Command($"select * from Stronger where NetID={NetID}");
            while (reader.Read())
            {
                strs.Add(new Stronger(NetID, reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),reader.GetInt32(7), JsonConvert.DeserializeObject<Item[]>(reader.GetString(8))));
            }
            return strs.ToArray();
        }
        /// <summary>
        /// 获取指定索引的强化
        /// </summary>
        /// <param name="index">该项强化的索引</param>
        /// <returns>若找到该项强化则返回该项强化的相关对象否则返回null</returns>
        public static Stronger GetStrongerByIndex(int index)
        {
            var reader = Data.Command($"select * from Stronger where id={index}");
            if (reader.Read())
                return new Stronger(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),reader.GetInt32(7), JsonConvert.DeserializeObject<Item[]>(reader.GetString(8)));
            else
                return null;
        }
        /// <summary>
        /// 更新一项强化，如果没有该项强化则向数据库中添加一条记录
        /// </summary>
        /// <param name="stronger">强化类</param>
        public static void Update(Stronger stronger)
        {
            var reader = Data.Command($"select * from Stronger where id={stronger.Index}");
            if (reader.Read())
            {
                Data.Command($"update Stronger set Damage={stronger.Damage},Knockback={stronger.Knockback},UseTime={stronger.UseTime},Size={stronger.Size},Probability={stronger.Probability},Stack={stronger.Stack},Needs='{JsonConvert.SerializeObject(stronger.Needs)}' where id={stronger.Index}");
            }
            else
            {
                var strs = GetStronger();
                int index = strs.Length;
                for(int i = 0; i < strs.Length; i++)
                {
                    if(strs[i].Index >= index)
                    {
                        index--;
                        break;
                    }
                }
                Data.Command($"insert into Stronger(NetID,Damage,Knockback,UseTime,Size,Probability,Stack,id,Needs)values({stronger.NetID},{stronger.Damage},{stronger.Knockback},{stronger.UseTime},{stronger.Size},{stronger.Probability},{stronger.Stack},{index},'{JsonConvert.SerializeObject(stronger.Needs)}')");
            }
        }
        /// <summary>
        /// 保存本对象
        /// </summary>
        public void Save()
        {
            Update(this);
        }
        public void Del()
        {
            Data.Command($"delete from Stronger where id={Index}");
        }
    }
    class Weapons
    {
        /// <summary>
        /// 玩家名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 武器ID
        /// </summary>
        public int NetID { get; set; }
        /// <summary>
        /// 增加的伤害百分比
        /// </summary>
        public int Damage { get; set; }
        /// <summary>
        /// 增加的击退百分比
        /// </summary>
        public int Knockback { get; set; }
        /// <summary>
        /// 减少的CD百分比
        /// </summary>
        public int UseTime { get; set; }
        /// <summary>
        /// 增加的大小百分比
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        /// 强化等级
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 创建一个新的强化武器对象
        /// </summary>
        /// <param name="name">玩家名</param>
        /// <param name="netID">武器id</param>
        /// <param name="damage">增加的伤害百分比</param>
        /// <param name="knockBack">增加的击退百分比</param>
        /// <param name="useTime">减少的CD百分比</param>
        /// <param name="size">增加的尺寸大小百分比</param>
        /// <param name="level">强化等级</param>
        public Weapons(string name,int netID,int damage,int knockBack,int useTime,int size,int level)
        {
            Name = name;
            NetID = netID;
            Damage = damage;
            Knockback = knockBack;
            UseTime = useTime;
            Size = size;
            Level = level;
        }
        /// <summary>
        /// 获取指定强化武器对象
        /// </summary>
        /// <param name="Name">玩家名</param>
        /// <param name="NetID">武器id</param>
        /// <returns>若成功获取则返回相关数据否则返回null</returns>
        public static Weapons GetWeapon(string Name,int NetID)
        {
            foreach (var w in GetWeapons(Name))
            {
                if (w.NetID == NetID)
                    return w;
            }
            return null;
        }
        /// <summary>
        /// 获取指定玩家的所有强化对象数组
        /// </summary>
        /// <param name="Name">玩家名</param>
        /// <returns>有关该玩家的所有强化武器数组</returns>
        public static Weapons[] GetWeapons(string Name)
        {
            var ws = new List<Weapons>();
            var reader = Data.Command($"select * from Weapons where Name='{Name}'");
            while (reader.Read())
            {
                ws.Add(new Weapons(Name, reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
            }
            return ws.ToArray();
        }
        /// <summary>
        /// 向数据库中更新一项武器强化，若无记录，则创建一条新的武器强化记录
        /// </summary>
        /// <param name="weapons">强化武器对象</param>
        public static void Update(Weapons weapons)
        {
            var reader = Data.Command($"select * from Weapons where Name='{weapons.Name}' and NetID={weapons.NetID}");
            if (reader.Read())
            {
                Data.Command($"update Weapons set Damage={weapons.Damage},Knockback={weapons.Knockback},UseTime={weapons.UseTime},Size={weapons.Size},Level={weapons.Level} where Name='{weapons.Name}' and NetID={weapons.NetID}");
            }
            else
            {
                Data.Command($"insert into Weapons(Name,NetID,Damage,Knockback,UseTime,Size,Level)values('{weapons.Name}',{weapons.NetID},{weapons.Damage},{weapons.Knockback},{weapons.UseTime},{weapons.Size},{weapons.Level})");
            }
        }
        /// <summary>
        /// 保存本对象
        /// </summary>
        public void Save()
        {
            Update(this);
        }
        public void Del()
        {
            Data.Command($"delete from Weapons where Name='{Name}' and NetID={NetID}");
        }
    }
}
