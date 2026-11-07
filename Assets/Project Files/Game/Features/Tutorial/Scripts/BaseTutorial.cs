using UnityEngine;

namespace NebulaSoft
{
    public abstract class BaseTutorial : MonoBehaviour, ITutorial
    {
        [SerializeField] 
        protected TutorialID tutorialId;
        public TutorialID TutorialID => tutorialId;

        [SerializeField] bool autoInitialise;

        public abstract bool IsActive { get; }
        public abstract bool IsFinished { get; }
        public abstract int Progress { get; }

        protected bool isInitialised;
        public bool IsInitialised => isInitialised;

        private void OnEnable()
        {
            TutorialController.RegisterTutorial(this);
        }

        private void OnDisable()
        {
            TutorialController.RemoveTutorial(this);
        }

        private void Start()
        {
            if (autoInitialise)
                Init();
        }

        public abstract void Init();

        public abstract void StartTutorial();
        public abstract void FinishTutorial();

        public abstract void Unload();
    }
}