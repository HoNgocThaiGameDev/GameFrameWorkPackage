using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Network Check", false)]
    public class NetworkCheckInitModule : InitModule
    {
        public override string ModuleName => "Network Check";

        [SerializeField] GameObject popupPrefab;

        public override void CreateComponent()
        {
            if(popupPrefab != null)
            {
                GameObject popupObject = Instantiate(popupPrefab);
                popupObject.name = "[Network Check Popup]";

                NetworkCheckPopup networkCheckPopup = popupObject.GetComponent<NetworkCheckPopup>();
                networkCheckPopup.Init();
            }
        }
    }
}