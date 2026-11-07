using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    [CustomEditor(typeof(Tween))]
    public class TweenEditor : CustomInspector
    {
        private readonly string[] tweenTypes = new string[] { "Update", "Fixed", "Late" };

        private int tabID = 0;

        private bool showTweens = false;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            showTweens = EditorGUILayout.Foldout(showTweens, "Show Active Tweens");

            if(showTweens)
            {
                tabID = GUILayout.Toolbar(tabID, tweenTypes);

                EditorGUILayout.BeginVertical();

                if(Tween.Tweens.IsInRange(tabID))
                {
                    TweensHolder tweensHolder = Tween.Tweens[tabID];
                    foreach (TweenCase tween in tweensHolder.Tweens)
                    {
                        if(tween != null)
                        {
                            if (GUILayout.Button(tween.ActiveID + ": " + tween.GetType()))
                            {
                                tween.Kill();

                                break;
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}
