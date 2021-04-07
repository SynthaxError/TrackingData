using System;
using UnityEngine;

namespace TrackingData
{
    static class Utils
    {
        public static GUIStyle LabelStyle(FontStyle style, Color color)
        {
            return new GUIStyle(GUI.skin.label) { fontStyle = style, normal = { textColor = color } };
        }
        public static void TwoValuesLabel(string name, string value)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, Utils.LabelStyle(FontStyle.Bold, Color.white));
                GUILayout.Label(value);
            }
            GUILayout.EndHorizontal();
        }
    }
}
