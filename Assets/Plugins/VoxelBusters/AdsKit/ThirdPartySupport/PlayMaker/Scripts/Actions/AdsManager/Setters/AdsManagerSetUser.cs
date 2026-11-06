#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Builds and applies User metadata for Ads Kit.")]
    public class AdsManagerSetUser : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("App user ID.")]
        public FsmString userId;

        [Tooltip("Optional user email.")]
        public FsmString email;

        [Tooltip("Optional first name.")]
        public FsmString firstName;

        [Tooltip("Optional last name.")]
        public FsmString lastName;

        [Tooltip("Set true to include gender in user object.")]
        public FsmBool useGender;

        [Tooltip("Gender value when UseGender is true.")]
        public FsmEnum gender;

        [Tooltip("Set true to include date of birth.")]
        public FsmBool useDateOfBirth;

        [Tooltip("Birth year (for example: 1995).")]
        public FsmInt birthYear;

        [Tooltip("Birth month (1-12).")]
        public FsmInt birthMonth;

        [Tooltip("Birth day (1-31).")]
        public FsmInt birthDay;

        [Tooltip("Guest user flag.")]
        public FsmBool isGuest;

        [ActionSection("Events")]
        [Tooltip("Event sent when user data is applied.")]
        public FsmEvent successEvent;

        [Tooltip("Event sent when validation fails.")]
        public FsmEvent failureEvent;

        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("Validation/exception message on failure.")]
        public FsmString errorMessage;

        public override void Reset()
        {
            userId = new FsmString { UseVariable = false };
            email = new FsmString { UseVariable = true };
            firstName = new FsmString { UseVariable = true };
            lastName = new FsmString { UseVariable = true };
            useGender = new FsmBool { UseVariable = false, Value = false };
            gender = new FsmEnum { UseVariable = false, EnumType = typeof(User.UserGender), Value = User.UserGender.Undefined };
            useDateOfBirth = new FsmBool { UseVariable = false, Value = false };
            birthYear = new FsmInt { UseVariable = false, Value = 2000 };
            birthMonth = new FsmInt { UseVariable = false, Value = 1 };
            birthDay = new FsmInt { UseVariable = false, Value = 1 };
            isGuest = new FsmBool { UseVariable = false, Value = false };
            successEvent = null;
            failureEvent = null;
            errorMessage = new FsmString { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            string id = userId.Value;
            if (string.IsNullOrEmpty(id))
            {
                SetFailure("UserId is null or empty.");
                return;
            }

            User.UserGender genderValue = (!useGender.IsNone && useGender.Value)
                ? (User.UserGender)gender.Value
                : User.UserGender.Undefined;

            DateTime? dob = null;
            if (!useDateOfBirth.IsNone && useDateOfBirth.Value)
            {
                try
                {
                    dob = new DateTime(birthYear.Value, birthMonth.Value, birthDay.Value);
                }
                catch (Exception ex)
                {
                    SetFailure($"Invalid date of birth: {ex.Message}");
                    return;
                }
            }

            var user = new User(userId: id,
                                email: email.IsNone ? null : email.Value,
                                firstName: firstName.IsNone ? null : firstName.Value,
                                lastName: lastName.IsNone ? null : lastName.Value,
                                gender: genderValue,
                                dob: dob,
                                isGuest: !isGuest.IsNone && isGuest.Value);

            AdsManager.SetUser(user);

            if (!errorMessage.IsNone)
            {
                errorMessage.Value = string.Empty;
            }

            Fsm.Event(successEvent);
            Finish();
#else
            SetFailure("Ads Kit is not enabled.");
#endif
        }

        private void SetFailure(string message)
        {
            Debug.LogError($"AdsManagerSetUser: {message}");
            if (!errorMessage.IsNone)
            {
                errorMessage.Value = message ?? string.Empty;
            }
            Fsm.Event(failureEvent);
            Finish();
        }
    }
}
#endif
