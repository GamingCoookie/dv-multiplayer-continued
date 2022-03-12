using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DVMultiplayer.Networking
{
    public static class FavoritesManager
    {
        private static readonly string path = "./Mods/DVMultiplayerContinued/Resources/Favorites.json";

        internal static void CreateFavoritesFileIfNotExists()
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }
        }

        internal static List<Favorite> GetFavorites()
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<Favorite>>(json);
        }

        internal static void SaveAsFavorite(string name, string hostname, int port)
        {
            string json = File.ReadAllText(path);
            List<Favorite> favorites;
            if (!string.IsNullOrWhiteSpace(json))
                favorites = JsonConvert.DeserializeObject<List<Favorite>>(json);
            else
                favorites = new List<Favorite>();

            if (favorites.Any(f => f.Name == name))
                throw new Exception("Favorite with this name already exists");

            favorites.Add(new Favorite()
            {
                Name = name,
                Hostname = hostname,
                Port = port
            });
            json = JsonConvert.SerializeObject(favorites, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        internal static Favorite Find(string name)
        {
            return GetFavorites().FirstOrDefault(fav => fav.Name == name);
        }

        internal static void Delete(string name)
        {
            string json = File.ReadAllText(path);
            List<Favorite> favorites = JsonConvert.DeserializeObject<List<Favorite>>(json);
            favorites.RemoveAll(f => f.Name == name);
            json = JsonConvert.SerializeObject(favorites, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }

    internal class Favorite
    {
        public string Name { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
    }
}
