using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Gift
{
    class Config
    {
        public GIFT[] Gifts { get; set; } = new GIFT[1] { new GIFT() };
        private const string path = "tshock/gifts.json";
        public static Config GetConfig()
        {
            var config = new Config();
            if (!File.Exists(path))
            {
                using (StreamWriter wr=new StreamWriter (path))
                {
                    wr.WriteLine(JsonConvert.SerializeObject(config,Formatting.Indented));
                }
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
    class GIFT
    {
        public string Name { get; set; } = "";
        public Gift.Item[] Items { get; set; } = new Gift.Item[1] { new Gift.Item() };
        public int Probability { get; set; } = 100;
    }
}
