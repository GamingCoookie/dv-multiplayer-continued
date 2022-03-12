using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.Utils.Game
{
    public static class SavedPositions
    {
        public static Dictionary<string, Vector3> Stations = new Dictionary<string, Vector3>()
        {
            { "Steel Mill", new Vector3(7922.636f, 131.8275f, 7341.672f) },
            { "Goods Factory & Town", new Vector3(12848.73f, 140.166f, 11047.32f) },
            { "Oil Well North", new Vector3(11553.89f, 122.33f, 11517.03f) },
            { "Food Factory & Town", new Vector3(9468.295f, 119.24f, 13615.79f) },
            { "Military Base", new Vector3(12756.73f, 215.12f, 14758.09f) },
            { "Iron Ore Mine East", new Vector3(14949.74f, 248.13f, 15254.15f) },
            { "Coal Mine", new Vector3(15586.77f, 204.27f, 11135.17f) },
            { "Harbor & Town", new Vector3(13060.85f, 113.1194f, 3519.413f) },
            { "Forrest South", new Vector3(5365.048f, 174.81f, 3713.231f) },
            { "Sawmill", new Vector3(1361.438f, 147.15f, 2288.875f) },
            { "City South West", new Vector3(2024.84f, 122.35f, 5671.265f) },
            { "Oil Well Central", new Vector3(4950.705f, 123, 6261.531f) },
            { "Farm", new Vector3(5990.341f, 123.8702f, 6743.591f) },
            { "Forrest Central", new Vector3(5688.753f, 145, 8691.468f) },
            { "Machine Factory & Town", new Vector3(2342.306f, 159.24f, 10970.78f) },
            { "Iron Ore Mine West", new Vector3(2017.741f, 133.63f, 13403.78f) }
        };
    }
}
