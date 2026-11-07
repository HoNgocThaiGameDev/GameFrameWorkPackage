using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    [RequireComponent(typeof(Button), typeof(BoxCollider))]
    public class WorldSpaceButton : MonoBehaviour
    {
        private Button button;
        private BoxCollider boxCollider;

        private bool IsInitialized { get; set; }

        private void Awake()
        {
            if (!IsInitialized) Init();
        }

        private void Init()
        {
            IsInitialized = true;

            button = GetComponent<Button>();
            boxCollider = GetComponent<BoxCollider>();
        }

        public bool Raycast(Ray ray)
        {
            return boxCollider.Raycast(ray, out _, 100);
        }

        public void Press(PointerEventData eventData)
        {
            button.OnPointerDown(eventData);
        }

        public void Press()
        {
            button.ClickButton();
        }

        public void Release(PointerEventData eventData)
        {
            button.OnPointerUp(eventData);
            button.OnPointerClick(eventData);
        }

        public void AddOnClickListener(UnityEngine.Events.UnityAction callback)
        {
            if (!IsInitialized) Init();

            button.onClick.AddListener(callback);
        }

        public void RemoveOnClickListener(UnityEngine.Events.UnityAction callback)
        {
            button.onClick.RemoveListener(callback);
        }

        private void OnEnable()
        {
            WorldSpaceRaycaster.AddWorldSpaceButton(this);
        }

        public void OnDisable()
        {
            WorldSpaceRaycaster.RemoveWorldSpaceButton(this);
        }

        private void OnValidate()
        {
            button = GetComponent<Button>();
            boxCollider = GetComponent<BoxCollider>();

            boxCollider.size = ((Vector3)button.image.rectTransform.sizeDelta).SetZ(1);
            boxCollider.isTrigger = true;
        }
    }
}