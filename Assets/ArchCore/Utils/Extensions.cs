using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text;
using ArchCore.DataStructures;
using Random = UnityEngine.Random;

namespace ArchCore.Utils
{
    public static class Extensions
    {
        #region String

        public static bool IsNullOrEmpty(this string str)
        {
            if (str == null) return true;
            return str.Length == 0;
        }

        public static string ToUpperFirstLetter(this string str)
        {
            string text = str.Substring(1);
            text = text.Insert(0, str[0].ToString().ToUpper());
            return text;
        }

        #endregion

        #region Numbers

        public static float Map(this float currentValue, float from, float to, float from2, float to2)
        {
            if (currentValue <= from2)
                return from;
            else if (currentValue >= to2)
                return to;
            return (to - from) * ((currentValue - from2) / (to2 - from2)) + from;
        }


        public static int Map(this int currentValue, int from, int to, int from2, int to2)
        {
            return from2 + (int) ((float) (to2 - from2) * ((float) (currentValue - from) / (float) (to - from)));
        }

        #endregion

        #region Random

        public static T PickRandom<T>(params T[] vals)
        {
            if (vals.Length == 0)
            {
                Debug.LogError("Please enter at least one value");
                return default(T);
            }

            return vals[UnityEngine.Random.Range(0, vals.Length)];
        }

        public static float NextFloat(this System.Random random)
        {
            return (float) random.NextDouble();
        }

        public static float NextFloat(this System.Random random, float minValue, float maxValue)
        {
            return minValue + (float) random.NextDouble() * (maxValue - minValue);
        }

        #endregion

        #region Vector

        public static Vector3 PerValueMul(this Vector3 vec, Vector3 vec2)
        {
            vec.x *= vec2.x;
            vec.y *= vec2.y;
            vec.z *= vec2.z;
            return vec;
        }

        #endregion

        #region RectTransform

        public static RectTransform GetRectTransform(this UnityEngine.Component obj)
        {
            return obj.transform as RectTransform;
        }

        #endregion


        #region Shared

        public static void Swap<T>(this List<T> list, T a, T b)
        {
            int aIndex = list.FindIndex(obj => obj.Equals(a));
            int bIndex = list.FindIndex(obj => obj.Equals(b));
            T tmp = list[aIndex];
            list[aIndex] = list[bIndex];
            list[bIndex] = tmp;
        }

        #endregion

        #region Textures

        public static Texture2D FillTexture(this Texture2D texture, Color color)
        {
            Texture2D texture2D = new Texture2D(texture.width, texture.height);
            texture2D.name = "ClearTexture";
            Color[] colors = new Color[texture2D.width * texture2D.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            texture2D.SetPixels(colors);
            return texture2D;
        }

        public static Texture2D TextureFromPath(this string path)
        {
            Texture2D tmp = null;
            byte[] fileData;
            if (!File.Exists(path))
            {
                return null;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int) fs.Length);
                fs.Flush();
                fs.Dispose();
                fs.Close();
            }

            //fileData = File.ReadAllBytes(path);
            tmp = new Texture2D(2, 2);
            tmp.LoadImage(fileData);
            return tmp;
        }

        public static void TextureFromPath(this string path, ref Texture2D texture)
        {
            byte[] fileData;
            if (!File.Exists(path))
            {
                return;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int) fs.Length);
                fs.Flush();
                fs.Dispose();
                fs.Close();
            }

            //fileData = File.ReadAllBytes(path);


            try
            {
                texture.LoadImage(fileData);
            }
            catch
            {
                texture = null;
            }
        }

        public static Sprite Texture2DToSprite(this Texture2D texture2D)
        {
            if (texture2D == null)
                return null;
            return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
        }

        public static Sprite Texture2DToSprite(this Texture2D texture2D, Vector2 pivot)
        {
            return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot);
        }

        #endregion

        #region Colors

        public static Color ColorFromString(this string colorText)
        {
            colorText = colorText.Trim();
            Color color;
            if (colorText.StartsWith("#"))
            {
                ColorUtility.TryParseHtmlString(colorText, out color);
            }
            else
            {
                color = colorText.ConvertRgbaToHex();
            }

            return color;
        }

        public static Color ConvertRgbaToHex(this string rgba)
        {
            string[] colorsContainer = rgba.Split('(', ')');
            string[] colors = colorsContainer[1].Split(',');
            float r = float.Parse(colors[0]) / 255f;
            float g = float.Parse(colors[1]) / 255f;
            float b = float.Parse(colors[2]) / 255f;
            Color color = new Color(r, g, b);
            if (colors.Length > 3)
            {
                float a = float.Parse(colors[3]);
                color = new Color(r, g, b, a);
            }

            return color;
        }

        #endregion

        #region Collections

        public static bool ContainsAll<T>(this IList<T> array1, IList<T> array2)
        {
            for (int i = 0; i < array2.Count; i++)
            {
                if (!array1.Contains(array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void DeleteCollection<T>(this ICollection<T> collection) where T : MonoBehaviour
        {
            foreach (T item in collection)
            {
                GameObject itemTransform = item.gameObject;
                Object.Destroy(itemTransform);
            }

            collection.Clear();
        }

        public static string PrintDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            string str = "";
            foreach (KeyValuePair<TKey, TValue> keyValuePair in dictionary)
            {
                str += string.Format("Key: {0}  --  Value: {1}\n", keyValuePair.Key, keyValuePair.Value);
            }

            return str;
        }

        public static string Print(this IEnumerable collection)
        {
            string str = "";
            foreach (var element in collection)
            {
                if (element != null)
                {
                    str += element.ToString() + "\n";
                }
                else
                {
                    str += "Null\n";
                }
            }

            return str;
        }
        
        public static string Print<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            string str = "";
            foreach (var element in collection)
            {
                if (element != null)
                {
                    str += toString(element) + "\n";
                }
                else
                {
                    str += "Null\n";
                }
            }

            return str;
        }

        public static bool IsNotEmpty<T>(ICollection collection)
        {
            return collection != null && collection.Count != 0;
        }

        public static List<T> RemoveNullObjectsFromList<T>(this List<T> list)
        {
            list.RemoveAll(obj => obj == null);
            return list;
        }

        public static T PickRandom<T>(this IEnumerable<T> list)
        {
            int count = list.Count();
            if (count == 0)
            {
                Debug.LogError("List is empty");
                return default(T);
            }

            return list.ElementAt(UnityEngine.Random.Range(0, count));
        }
        
        public static T PickRandomParamByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);
            if (Mathf.Abs(sequence.Sum(weightSelector) - 1f) >= 0.01f)
            {
                Debug.LogWarning(
                    $"Incorrect sum of weights! Sum must be equals to 1f, at sequence - sum is {totalWeight}");
            }

            return PickRandomByWeight(sequence, weightSelector);
        }
        
        public static T PickRandomByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);

            float itemWeightIndex = Random.Range(0f, 1f) * totalWeight;
            float currentWeightIndex = 0f;

            foreach (var item in from weightedItem in sequence
                select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;
            }

            return sequence.LastOrDefault();
        }

        public static T PickRandom<T>(this IEnumerable<T> list, System.Random random)
        {
            int count = list.Count();
            if (count == 0)
            {
                Debug.LogError("List is empty");
                return default(T);
            }

            return list.ElementAt(random.Next(0, count));
        }

        public static T PickRandom<T>(this IEnumerable<T> list, params float[] probabilityValues)
        {
            int count = list.Count();
            if (count == 0)
            {
                Debug.LogError("List is empty");
                return default(T);
            }

            float r = UnityEngine.Random.Range(0, probabilityValues.Sum());

            float s = 0;
            for (int i = 0; i < probabilityValues.Length; i++)
            {
                s += probabilityValues[i];
                if (r < s)
                {
                    return list.ElementAt(i);
                }
            }

            return list.Last();
        }

        public static T PickRandom<T>(this IEnumerable<T> list, System.Random random, params float[] probabilityValues)
        {
            int count = list.Count();
            if (count == 0)
            {
                Debug.LogError("List is empty");
                return default(T);
            }

            float r = random.NextFloat(0, probabilityValues.Sum());

            float s = 0;
            for (int i = 0; i < probabilityValues.Length; i++)
            {
                s += probabilityValues[i];
                if (r < s)
                {
                    return list.ElementAt(i);
                }
            }

            return list.Last();
        }
        

        public static void Insert<T>(this LinkedList<T> list, int position, T item)
        {
            if (list.Count <= position)
            {
                list.AddLast(item);
                return;
            }

            LinkedListNode<T> curr = list.First;
            int i = 0;
            while (i < position)
            {
                curr = curr.Next;
                i++;
            }

            list.AddBefore(curr, item);
        }
        
        public static IList<T> Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }

            return list;
        }
	
        public static IList<T> Shuffle<T>(this IList<T> list, System.Random random)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = random.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
            return list;
        }
        
        public static ReadOnlyList<T> ToReadonlyList<T>(this IEnumerable<T> collection)
        {
            if (collection is List<T> list) return new ReadOnlyList<T>(list);
            return new ReadOnlyList<T>(collection.ToList());
        }
        
        #endregion

        #region Rest

        public static string MakePageableUrl(this string url, long count, long page = 0)
        {
            return $"{url}?page={page}&size={count}";
        }

        #endregion
        
        public static bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        public static IEnumerator TrackProgress(this AsyncOperation operation, ProgressAsyncTask progressAsyncTask)
        {
            while (operation.progress < 0.99)
            {
                yield return null;
                progressAsyncTask.Progress(operation.progress);
            }

            yield return null;
            progressAsyncTask.Progress(1);
        }


        public static string GetPathHierarchy(this Transform transform, int deep = 3)
        {
            StringBuilder path = new StringBuilder();
            while (transform && deep >= 0)
            {
                path.Insert(0, transform.name);
                path.Insert(0, ">");
                transform = transform.parent;
                deep--;
            }

            return path.ToString();
        }

        private static IEnumerable<Type> allTypesChached;
        
        public static Type[] GetDerivedTypes(this Type baseType, bool includeAbstract = false, params Assembly[] assemblies)
        {
            IEnumerable<Type> types;
            if (assemblies.NullOrEmpty())
            {
                types = baseType.Assembly.GetTypes();
            }
            else
            {
                types = assemblies.SelectMany(s => s.GetTypes());
            }
            return types.Where(pp =>
            {
                if (!baseType.IsAssignableFrom(pp) || pp.IsInterface || !pp.IsClass) return false;
                if (pp.IsAbstract)
                    return includeAbstract;
                return true;
            }).ToArray();
        }
        
        public static bool NullOrEmpty(this Array array) => array == null || array.Length == 0;
        
        public static Type[] GetDerivedTypesInAllAssemblies(this Type baseType, bool includeAbstract = false)
        {
            if(allTypesChached == null)
                allTypesChached = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes());
            return allTypesChached.Where(pp => baseType.IsAssignableFrom(pp) && !pp.IsInterface && pp.IsClass && (!includeAbstract || !pp.IsAbstract)).ToArray();
        }
        
        public static string ToQueryString(this Dictionary<string, string> allParams)
        {
            return string.Join("&", allParams.Select(param => $"{param.Key}={param.Value}").ToArray());
        }
        
    }
}

namespace UnityEngine
{
    public static class UnityEngineExtensions
    {
        public static bool UnityNullCheck(this System.Object obj)
        {
            return obj == null || obj.Equals(null);
        }

#if !HEADLESS
        public static void SetPreset(this AudioSource sourcePreset, AudioSource targetPreset)
        {
            targetPreset.pitch = sourcePreset.pitch;
            targetPreset.priority = sourcePreset.priority;
            targetPreset.spatialize = sourcePreset.spatialize;
            targetPreset.spread = sourcePreset.spread;

            targetPreset.playOnAwake = false;
            
            targetPreset.volume = sourcePreset.volume;
            
            targetPreset.dopplerLevel = sourcePreset.dopplerLevel;
            
            targetPreset.maxDistance = sourcePreset.maxDistance;
            targetPreset.minDistance = sourcePreset.minDistance;
            
            targetPreset.panStereo = sourcePreset.panStereo;
            targetPreset.rolloffMode = sourcePreset.rolloffMode;
            targetPreset.spatialBlend = sourcePreset.spatialBlend;

            targetPreset.reverbZoneMix = sourcePreset.reverbZoneMix;
            targetPreset.spatializePostEffects = sourcePreset.spatializePostEffects;
            targetPreset.velocityUpdateMode = sourcePreset.velocityUpdateMode;
            
            targetPreset.outputAudioMixerGroup = sourcePreset.outputAudioMixerGroup;

            targetPreset.SetCustomCurve(AudioSourceCurveType.CustomRolloff, sourcePreset.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            targetPreset.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, sourcePreset.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
            targetPreset.SetCustomCurve(AudioSourceCurveType.SpatialBlend, sourcePreset.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
            targetPreset.SetCustomCurve(AudioSourceCurveType.Spread, sourcePreset.GetCustomCurve(AudioSourceCurveType.Spread));
        }
#endif

    }
}