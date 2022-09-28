#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArchCore.Utils
{
    public class PositionDrawer : MonoBehaviour
    {
        Gradient gradient;
        GradientColorKey[] colorKey;
        GradientAlphaKey[] alphaKey;

        private int drawCount = 500;

        readonly Queue<(string text, Vector3 pos)> drawPos = new Queue<(string text, Vector3 pos)>();

        private float maxRadius = .5f;
        private float minRadius = .1f;


        public static PositionDrawer Create(int posCount)
        {
            var go = new GameObject();
            var d = go.AddComponent<PositionDrawer>();
            d.Init(posCount);
            d.SetMinMaxRadius(.1f, .5f);
            return d;
        }

        public void SetMinMaxRadius(float min, float max)
        {
            minRadius = min;
            maxRadius = max;
        }

        public void AddPos(Vector3 pos, string text)
        {
            (string text, Vector3 pos) tuple;
            tuple.pos = pos;
            tuple.text = text;
            drawPos.Enqueue(tuple);
            if (drawPos.Count >= drawCount)
                drawPos.Dequeue();
            //RemoveDuplicatesIterative(drawPos.ToList());
        }

        public void RemoveDuplicatesIterative(List<(string text, Vector3 pos)> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                // Assume not duplicate.
                bool duplicate = false;
                for (int z = 0; z < i; z++)
                {
                    if (items[z].pos == items[i].pos && !items[z].text.Equals(items[i].text))
                    {
                        Debug.LogWarning($"!!! pos:{items[i].pos} f1:{items[i].text} , f2:{items[z].text}");
                        break;
                    }
                }
            }
        }

        private void CheckForDublicates()
        {
            foreach (var tuple1 in drawPos)
            {
                foreach (var tuple2 in drawPos)
                {
                    if (tuple1.pos == tuple2.pos)
                        Debug.LogWarning($"!!! pos:{tuple1.pos}, f1:{tuple1.text}, f1:{tuple1.text}");
                }
            }
        }

        private Vector3 CalculateTextPos(Vector3 pos, float radius)
        {
            var offset = radius + radius * .1f;
            return pos + new Vector3(offset, -offset, 0);
        }

        private float CalculateRadius(float coef)
        {
            return Mathf.Clamp(coef * maxRadius, minRadius, maxRadius);
        }

        private void OnDrawGizmos()
        {
            (string text, Vector3 pos)[] queueAr = drawPos.ToArray();
            for (int i = 0; i < queueAr.Length; i++)
            {
                float coef = (float) i / (float) queueAr.Length;
                var radius = CalculateRadius(coef);
                Gizmos.color = gradient.Evaluate(coef);
                Gizmos.DrawSphere(queueAr[i].pos, radius);
                if (!string.IsNullOrEmpty(queueAr[i].text))
                {
                    Handles.Label(CalculateTextPos(queueAr[i].pos, radius), queueAr[i].text);
                }
            }
        }

        private void Init(int posCount)
        {
            drawCount = posCount;
            gradient = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = Color.red;
            colorKey[0].time = 0.0f;
            colorKey[1].color = Color.blue;
            colorKey[1].time = 1.0f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 0.1f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1f;
            alphaKey[1].time = 1.0f;

            gradient.SetKeys(colorKey, alphaKey);
        }
    }
}
#endif