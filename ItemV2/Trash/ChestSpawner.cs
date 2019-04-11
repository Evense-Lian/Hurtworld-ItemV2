using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Random = UnityEngine.Random;
using Oxide;

namespace Oxide.Plugins
{
    [Info("ChestSpawner", "Zoond Engine", "1.2.0")]
    [Description("Ставит сундуки на карте в рандомном порядке")]

    class ChestSpawner : HurtworldPlugin
    {
        Dictionary<string, GameObject> Chests =
            new Dictionary<string, GameObject>();

        int[] ExtraItems = new int[2];

        GlobalItemManager GIM = Singleton<GlobalItemManager>.Instance;

        public class Coordinates
        {
            public int Id { get; }
            public float X { get; }
            public float Y { get; }
            public float Z { get; }

            public Coordinates(int id, float x, float y, float z)
            {
                Id = id;
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// Регионы появлений сундуков
        /// </summary>
        public struct Countries
        {
            public static int IN_BALL = 0;
            public static int AROUND_BALL = 1;
            public static int RT2WB = 2;
            public static int RT2DB = 3;
            public static int IN_CARRIER = 4;
            public static int ON_CARIIER = 5;
            public static int RT1FB = 6;
            public static int RT1WB = 7;
            public static int AERO = 8;
            public static int UFO = 9;
        }

        /// <summary>
        /// Имена согласно регионов
        /// </summary>
        public struct CountriesNames
        {
            public static string IN_BALL = "В шаре";
            public static string AROUND_BALL = "Вокруг шара";
            public static string RT2WB = "Зимний биом: [PT2]";
            public static string RT2DB = "Красная пустыня: [PT2]";
            public static string IN_CARRIER = "В авианосце";
            public static string ON_CARIIER = "На авианосце";
            public static string RT1FB = "Лесной биом: [PT1]";
            public static string RT1WB = "Зимний биом: [РТ1]";
            public static string AERO = "Аэропорт";
            public static string UFO = "НЛО";
        }

        /// <summary>
        /// Ответы от плагина в сервер или на команду
        /// </summary>
        public struct Responses
        {
            public static string MSG_PLAYER_IS_NOT_ADMIN = "Вы не являетесь админстратором чтобы использовать эту команду !";
        }

        /// <summary>
        /// Позиции появлений сундуков
        /// </summary>
        public struct Positions
        {
            //Создаем хранилище
            public static List<Coordinates> Coords = new List<Coordinates>();
        }

        /// <summary>
        /// Номера вещей используемых в плагине
        /// </summary>
        public struct Items
        {
            //Уникальная вещь
            public static int RUBY = 12;

            //Коммон вещи
            public static int AMBER = 118;


            //Экстра вещи
            public static int C4 = 144; //1
            public static int PRIMER = 231; //2
            public static int DRILL_RAIDING = 318; //3
        }

        /// <summary>
        /// Количества вещей используемых в плагине
        /// </summary>
        public struct Counts
        {
            //Ранжи
            public static int DEFUALT = 1;
            public static int RANGE_MIN = 150;
            public static int RANGE_MAX = 300;

            //Рубин
            public static int RUBY_MIN = 2;
            public static int RUBY_MAX = 10;
        }

        void Loaded()
        {
            permission.RegisterPermission("chestspawner.admin", this);

            BootCoordinates();

            Initialize();

            Puts("Таймер на восстановление сундуков инициализирован");
            timer.Repeat(21600, 999, () => {
                Initialize();
            });
        }

        private void Initialize()
        {
            Destroy();

            BootPlugin();
        }

        private void BootCoordinates()
        {
            Positions.Coords.Add(new Coordinates(0, (float)12.51339, (float)375.75, (float)-1366.333));
            Positions.Coords.Add(new Coordinates(1, (float)-2944.2, (float)198.2, -1072));
            Positions.Coords.Add(new Coordinates(2, 1194, 172, 1252));
            Positions.Coords.Add(new Coordinates(3, 1946, 192, 1481));
            Positions.Coords.Add(new Coordinates(4, -946, 206, -2405));
            Positions.Coords.Add(new Coordinates(5, -836, 233, -2418));
            Positions.Coords.Add(new Coordinates(6, -3304, 197, -1598));
            Positions.Coords.Add(new Coordinates(7, -2640, 170, 1663));
            Positions.Coords.Add(new Coordinates(8, 1257, 200, -3347));
            Positions.Coords.Add(new Coordinates(9, -2534, 216, 225));
        }

        private void BootPlugin()
        {
            Puts("Сундуки поставлены");

            int clone = 0;
            int i = 0;

            while (i != 3)
            {
                var number = 0;

                while (Chests.ContainsKey(number.ToString()))
                {
                    number = Random.Range(0, 9);
                }

                clone = number;

                var coordinate = Positions.Coords.Where((c) => c.Id == number).FirstOrDefault();

                Spawn(coordinate.X, coordinate.Y, coordinate.Z, coordinate.Id, i);
                i++;
            }
        }

        private void Spawn(float x, float y, float z, int location, int iterator)
        {
            string chest = "storegaLocker";
            //NetworkInstantiateConfig test123 = RuntimeHurtDB.Instance.GetObjectByGuid(chest);



            Vector3 pos = new Vector3(x, y, z);

            RaycastHit hitInfo;

            if (Physics.Raycast(pos, Vector3.down, out hitInfo))
            {
                /*
                // Singleton<RuntimeAssetManager>.Instance.RefList.LootCachePrefab
                Quaternion rotation = Quaternion.Euler(0.0f, (float)UnityEngine.Random.Range(0f, 360f), 0.0f);
                rotation = Quaternion.FromToRotation(Vector3.down, hitInfo.normal) * rotation;
                GameObject gameObject = Singleton<HNetworkManager>.Instance.NetInstantiate(uLink.NetworkPlayer.server, Singleton<RuntimeAssetManager>.Instance., hitInfo.point, Quaternion.identity, GameManager.GetSceneTime());

                if (gameObject != null)
                {
                    Chests.Add(iterator.ToString(), gameObject);
                    Inventory inv = gameObject.GetComponent<Inventory>() as Inventory;
                    if (inv.Capacity < 8)
                        inv.SetCapacity(8);
                    uLink.NetworkView nwv = uLink.NetworkView.Get(gameObject);
                    AppendItems(inv);
                }*/
            }

            //Server.Broadcast("<color=#F8620D>Феникс</color> оставил клад в: <color=#C908AA><" + GetLocationName(location) + "></color> (<color=#53F915>пропадет через: 6 часов</color>)");
        }

        private void AppendItems(Inventory inv)
        {
            AddItem(inv, Items.AMBER, Counts.RANGE_MIN, Counts.RANGE_MAX);
            //Extra
            AddItem(inv, ExtraRandom(), 1, 1);
        }

        void AddItem(Inventory inv, int itemId, int min, int max)
        {
            Dictionary<int, ItemGeneratorAsset> itemGenerators = Singleton<GlobalItemManager>.Instance.ItemGenerators;
            int val = Random.Range(min, max);
            var item = GIM.GetItem(itemId);
            ItemGeneratorAsset generator = itemGenerators[itemId];
            GIM.GiveItem(generator, val, inv);
        }




        private void Destroy()
        {
            foreach (KeyValuePair<string, GameObject> obj in Chests)
            {
                // Singleton<HNetworkManager>.Instance.NetDestroy(uLink.NetworkView.Get(obj.Value));
                Singleton<HNetworkManager>.Instance.NetDestroy(HNetworkExtensions.HNetworkView(obj.Value));

            }

            Chests.Clear();

            Puts("Все сундуки уничтожены");
        }

        private void AdminSpawn()
        {
            Destroy();

            BootPlugin();
        }

        private int ExtraRandom()
        {
            int item = 144;

            foreach (int extra in ExtraItems)
            {
                while (item == extra)
                {
                    item = Random.Range(0, 2);
                    item = GetExtra(item);
                }
                break;
            }

            return item;
        }

        private int GetExtra(int id)
        {
            int item = 0;

            switch (id)
            {
                case 0:
                    {
                        item = Items.C4;
                        break;
                    }

                case 1:
                    {
                        item = Items.PRIMER;
                        break;
                    }

                case 2:
                    {
                        item = Items.DRILL_RAIDING;
                        break;
                    }
            }

            return item;
        }

        private string GetLocationName(int id)
        {
            string location = null;

            switch (id)
            {
                case 0:
                    {
                        location = CountriesNames.IN_BALL;
                        break;
                    }

                case 1:
                    {
                        location = CountriesNames.AROUND_BALL;
                        break;
                    }

                case 2:
                    {
                        location = CountriesNames.RT2WB;
                        break;
                    }

                case 3:
                    {
                        location = CountriesNames.RT2DB;
                        break;
                    }

                case 4:
                    {
                        location = CountriesNames.IN_CARRIER;
                        break;
                    }

                case 5:
                    {
                        location = CountriesNames.ON_CARIIER;
                        break;
                    }

                case 6:
                    {
                        location = CountriesNames.RT1FB;
                        break;
                    }

                case 7:
                    {
                        location = CountriesNames.RT1WB;
                        break;
                    }

                case 8:
                    {
                        location = CountriesNames.AERO;
                        break;
                    }

                case 9:
                    {
                        location = CountriesNames.UFO;
                        break;
                    }
            }

            return location;
        }

        //Команды
        [ChatCommand("csclear")]
        void stopchestspawn(PlayerSession player, string command, string[] args)
        {
            SendMessageToPlayer(player, "Клад успешно очищен");
            SendBroadcastMessage("Администратор " + player.Identity.Name + " очистил клад");
            Destroy();
        }

        [ChatCommand("csspawn")]
        void CssspawnCommand(PlayerSession player, string command, string[] args)
        {
            SendMessageToPlayer(player, "Клад успешно выставлен");
            SendBroadcastMessage("Администратор " + player.Identity.Name + " выставил клад");
            AdminSpawn();
        }

        private void GetConfig<T>(ref T variable, params string[] path)
        {
            if (path.Length == 0)
                return;

            if (Config.Get(path) == null)
            {
                Config.Set(path.Concat(new object[] { variable }).ToArray());
                PrintWarning($"Added field to config: {string.Join("/", path)}");
            }

            variable = (T)Convert.ChangeType(Config.Get(path), typeof(T));
        }

        //Алиас для метода отправки сообщения в чат человеку
        void SendMessageToPlayer(PlayerSession session, string message) => hurt.SendChatMessage(session, null, message);

        //Алиас метода бродкаста
        void SendBroadcastMessage(string message) => hurt.BroadcastChat(null, message);
    }
}