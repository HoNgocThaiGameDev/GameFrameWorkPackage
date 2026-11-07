using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(ParticlesController))]
    public class ParticlesControllerEditor : CustomInspector
    {
        private Dictionary<int, Particle> registeredParticles;

        protected override void OnEnable()
        {
            base.OnEnable();

            object boxedRegisteredParticles = ReflectionUtils.GetStaticComponent<ParticlesController>("registerParticles");
            if(boxedRegisteredParticles != null)
            {
                registeredParticles = (Dictionary<int, Particle>)boxedRegisteredParticles; 
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(Application.isPlaying)
            {
                if (registeredParticles != null)
                {
                    int registeredParticlesCount = registeredParticles.Count;
                    if (registeredParticlesCount > 0)
                    {
                        EditorGUILayout.BeginVertical(EditorCustomStyles.box);
                        EditorGUILayout.LabelField(string.Format("Registered particles: {0}", registeredParticlesCount));

                        foreach (Particle registeredParticle in registeredParticles.Values)
                        {
                            EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);

                            EditorGUILayout.LabelField(registeredParticle.ParticleName, GUILayout.Width(80));

                            GUILayout.FlexibleSpace();

                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.ObjectField(GUIContent.none, registeredParticle.ParticlePrefab, typeof(GameObject), allowSceneObjects: false, GUILayout.Width(80));
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }
    }
}
