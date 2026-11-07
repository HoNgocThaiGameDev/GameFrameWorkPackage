using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class UIExtensions
    {
        public static void ClickButton(this Button button)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);

            Tween.NextFrame(() =>
            {
                var eventData = new PointerEventData(EventSystem.current);

                eventData.button = PointerEventData.InputButton.Left;
                button.OnPointerClick(eventData);

                Tween.DelayedCall(0.2f, () => EventSystem.current.SetSelectedGameObject(null));
            }, unscaledTime: true);
        }

        public static void AddEvent(this Component behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            AddEvent(behaviour.gameObject, triggerType, call);
        }

        public static void AddEvent(this GameObject behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            EventTrigger trigger = behaviour.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = behaviour.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener((data) => { call((PointerEventData)data); });

            trigger.triggers.Add(entry);
        }
    }
}
