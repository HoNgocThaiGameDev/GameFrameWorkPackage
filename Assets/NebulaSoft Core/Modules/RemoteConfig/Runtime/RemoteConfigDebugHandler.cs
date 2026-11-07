using System;
using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    public class RemoteConfigDebugHandler : RemoteConfigHandler
    {
        [SerializeField] string debugConfig;

        private bool isLoaded;

        public override bool IsLoaded()
        {
            return isLoaded;
        }

        protected override void Init()
        {
            StartCoroutine(WaitCoroutine());
        }

        private IEnumerator WaitCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.5f);

            isLoaded = true;
        }

        protected override void OnConfigLoaded()
        {
            RemoteConfigController.Init(debugConfig);
        }

        protected override void OnTimeout()
        {

        }
    }
}
