using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public class TutorialController : MonoBehaviour
    {
        private static TutorialController tutorialController;
        private static List<ITutorial> registeredTutorials = new List<ITutorial>();

        [SerializeField] TutorialCanvasController tutorialCanvasController;

        private static bool isTutorialSkipped;

        public void Init()
        {
            tutorialController = this;

            isTutorialSkipped = TutorialHelper.IsTutorialSkipped();

            tutorialCanvasController.Init();
        }

        private void OnDestroy()
        {
            if(!registeredTutorials.IsNullOrEmpty())
            {
                foreach(ITutorial tutorial in registeredTutorials)
                {
                    tutorial.Unload();
                }

                registeredTutorials.Clear();
            }
        }

        public static ITutorial GetTutorial(TutorialID tutorialID)
        {
            for(int i = 0; i < registeredTutorials.Count; i++)
            {
                if (registeredTutorials[i].TutorialID == tutorialID)
                {
                    if (!registeredTutorials[i].IsInitialised)
                        registeredTutorials[i].Init();

                    if (isTutorialSkipped)
                        registeredTutorials[i].FinishTutorial();

                    return registeredTutorials[i];
                }
            }

            Debug.LogError($"Tutorial with id {tutorialID} can't be found!");

            return null;
        }

        public static void ActivateTutorial(ITutorial tutorial)
        {
            if (!tutorial.IsInitialised)
                tutorial.Init();

            if (isTutorialSkipped)
                tutorial.FinishTutorial();
        }

        public static void RegisterTutorial(ITutorial tutorial)
        {
            if (registeredTutorials.FindIndex(x => x == tutorial) != -1)
                return;

            // Add tutorial to list
            registeredTutorials.Add(tutorial);
        }

        public static void RemoveTutorial(ITutorial tutorial)
        {
            int tutorialIndex = registeredTutorials.FindIndex(x => x == tutorial);
            if (tutorialIndex != -1)
            {
                // Remove tutorial from list
                registeredTutorials.RemoveAt(tutorialIndex);
            }
        }
    }
}