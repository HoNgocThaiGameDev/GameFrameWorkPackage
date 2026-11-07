using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public class TutorialBaseSave : ISaveObject
    {
        public bool isActive;
        public bool isFinished;

        public int progress;

        public void Flush()
        {

        }
    }
}