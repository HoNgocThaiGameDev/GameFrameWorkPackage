using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft.List
{
    [CreateAssetMenu(fileName = "StylesDatabase", menuName = "Data/Core/Editor/List Styles Database", order = 2)]
    public class ListStylesDatabase : ScriptableObject
    {
        [SerializeField] List<CustomListStyle> styles;

        [Button]
        private void AddDefaultStyle()
        {
            CustomListStyle style = new CustomListStyle();
            style.SetDefaultStyleValues();
            styles.Add(style);
        }

        public CustomListStyle GetStyle(int index)
        {
            return styles[Mathf.Clamp(index, 0, styles.Count - 1)];
        }
    }

}
