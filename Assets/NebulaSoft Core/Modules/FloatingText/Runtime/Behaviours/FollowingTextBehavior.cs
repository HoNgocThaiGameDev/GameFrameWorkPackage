using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    public class FollowingTextBehavior : FloatingTextBaseBehavior
    {
        [SerializeField] TMP_Text floatingText;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        private Vector3 defaultScale;
        private Transform followTransform;
        private Vector3 followOffset;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public void Activate(Transform followTransform, Vector3 followOffset)
        {
            this.followTransform = followTransform;
            this.followOffset = followOffset;

            transform.localScale = Vector3.zero;
            transform.DOScale(defaultScale, scaleTime).SetCurveEasing(scaleAnimationCurve);
        }

        private void Update()
        {
            if (followTransform == null)
                return;

            transform.position = followTransform.position + followOffset;
        }

        public void Unload()
        {
            followTransform = null;
        }
    }
}
