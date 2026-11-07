using UnityEngine;

namespace NebulaSoft
{
    public class SettingsElementsGroup : MonoBehaviour 
    { 
        public bool IsGroupActive()
        {
            int childCount = transform.childCount;
            for(int i = 0; i < childCount; i++)
            {
                if(transform.GetChild(i).gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
