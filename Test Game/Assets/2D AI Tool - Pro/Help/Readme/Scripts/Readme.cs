using System;
using UnityEngine;

namespace MaykerStudio.Help
{
    public class Readme : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        [ReorderableList]
        public Section[] sections;
        public bool hasShowedOnce;

        [Serializable]
        public class Section
        {
            public string heading, text, linkText, url;
        }

    }
}
