#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArchCore.Utils
{
    
    public static class EnumGenerator
    {

        public static void GenerateEnum(string name, string[] fields, string path)
        {

            if (fields == null || fields.Length == 0)
            {
                Debug.LogError("Your fields array is empty");
                return;
            }
            
            string[] uniqueFields = new string[fields.Length];
            Array.Copy(fields, uniqueFields, fields.Length);

            int n = uniqueFields.Length;

            string[] charsToRemove =
            {
                "@", ",", ".", ";", "'", "/", "\'", "!", "@", "#", "$", "%", "%", "^", "&", "*", "(", ")", "+", "=",
                "{", "}", "[", "]", "`", "~", "±", "§", ":", "?"
            };

            for (int i = 0; i < n; i++)
            {
                int index = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i != j && uniqueFields[i] == uniqueFields[j])
                    {
                        index++;
                        uniqueFields[j] += "_" + index;
                    }
                }

                for (int j = 0; j < charsToRemove.Length; j++)
                {
                    if (uniqueFields[i].Contains(charsToRemove[j]))
                    {
                        uniqueFields[i] = uniqueFields[i].Replace(charsToRemove[j], String.Empty);
                    }
                }

                if (uniqueFields[i].Contains(" "))
                {
                    uniqueFields[i] = uniqueFields[i].Replace(" ", "_");
                }

                if (uniqueFields[i].Contains("-"))
                {
                    uniqueFields[i] = uniqueFields[i].Replace("-", "_");
                }
            }
            
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(path);
            }

            

            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                streamWriter.WriteLine("public enum " + name);
                streamWriter.WriteLine("{");
                for (int i = 0; i < uniqueFields.Length; i++)
                {
                    streamWriter.WriteLine("\t" + uniqueFields[i] + ",");
                }

                streamWriter.WriteLine("}");
            }
           
            AssetDatabase.Refresh();
        }

    }
}
#endif