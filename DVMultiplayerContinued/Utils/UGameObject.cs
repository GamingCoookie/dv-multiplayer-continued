using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DVMultiplayer.Utils
{
    public static class UGameObject
    {
        public static bool Exists(string name)
        {
            return GameObject.Find(name) != null;
        }

        public static IEnumerable<Transform> FindAll(this Transform transform, string name)
        {
            List<Transform> transforms = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (Regex.IsMatch(child.gameObject.name, name))
                {
                    transforms.Add(child);
                }
            }
            return transforms;
        }

        public static IEnumerable<T> GetComponent<T>(this IEnumerable<Transform> transforms)
        {
            List<T> components = new List<T>();
            foreach (Transform transform in transforms)
            {
                components.Add(transform.GetComponent<T>());
            }
            return components;
        }
    }
}
