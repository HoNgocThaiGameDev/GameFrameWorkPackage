using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NebulaSoft.Tests
{
    public sealed class TweenLogicTests
    {
        private static readonly FieldInfo HoldersField = typeof(Tween).GetField("tweens", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo DriverField = typeof(Tween).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        private TweensHolder[] previousHolders;
        private TweensHolder[] holders;
        private TweensHolder UpdateHolder => holders[0];

        [SetUp]
        public void SetUp()
        {
            previousHolders = (TweensHolder[])HoldersField.GetValue(null);
            holders = new[] { new TweensHolder(UpdateMethod.Update, 16), new TweensHolder(UpdateMethod.FixedUpdate, 16), new TweensHolder(UpdateMethod.LateUpdate, 16) };
            HoldersField.SetValue(null, holders);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var holder in holders) holder.Unload();
            HoldersField.SetValue(null, previousHolders);
        }

        [Test]
        public void LateUpdate_OnlyTicksLateTweens()
        {
            var owner = new GameObject("Tween phase test");
            try
            {
                var driver = owner.AddComponent<Tween>();
                var previousDriver = DriverField.GetValue(null);
                int fixedCalls = 0, lateCalls = 0;
                Tween.DoWaitForCondition(_ => fixedCalls++, true, UpdateMethod.FixedUpdate);
                Tween.DoWaitForCondition(_ => lateCalls++, true, UpdateMethod.LateUpdate);
                try
                {
                    DriverField.SetValue(null, driver);
                    typeof(Tween).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);
                }
                finally { DriverField.SetValue(null, previousDriver); }
                Assert.That(fixedCalls, Is.Zero);
                Assert.That(lateCalls, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Delay_ConsumesOnlyItsShareOfTheTick(bool unscaled)
        {
            float value = 0;
            Tween.DoFloat(0, 1, 1, next => value = next, 0.25f, unscaled);
            UpdateHolder.Update(unscaled ? 0.1f : 0.5f, 0.5f);
            Assert.That(value, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void LastTick_EmitsEveryCrossedMilestoneAndFinalValueOnce()
        {
            var events = new List<string>();
            var tween = Tween.DoFloat(0, 10, 1, value => events.Add("value:" + value));
            tween.OnTimeReached(1, () => events.Add("end"));
            tween.OnTimeReached(0.5f, () => events.Add("half"));
            tween.OnComplete(() => events.Add("complete"));

            UpdateHolder.Update(2, 2);
            UpdateHolder.Update(2, 2);

            CollectionAssert.AreEqual(new[] { "half", "end", "value:10", "complete" }, events);
            Assert.That(tween.State, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDuration_WithZeroDeltaStillAppliesFinalValueOnce()
        {
            var values = new List<float>();
            Tween.DoFloat(0, 10, 0, values.Add, unscaledTime: true);
            UpdateHolder.Update(0, 0);
            CollectionAssert.AreEqual(new[] { 10f }, values);
        }

        [Test]
        public void KillDuringMilestone_SkipsUpdateAndCompletion()
        {
            int values = 0, completions = 0;
            var tween = Tween.DoFloat(0, 1, 1, _ => values++);
            tween.OnTimeReached(0.25f, () => tween.Kill()).OnComplete(() => completions++);
            UpdateHolder.Update(0.5f, 0.5f);
            Assert.That(values, Is.Zero);
            Assert.That(completions, Is.Zero);
            Assert.That(tween.ActiveID, Is.EqualTo(-1));
        }

        [Test]
        public void TweensAddedFromCompletion_WaitUntilTheNextTick()
        {
            int childCalls = 0;
            Tween.DelayedCall(0.1f, () => Tween.DoFloat(0, 1, 0.1f, _ => childCalls++));
            UpdateHolder.Update(1, 1);
            Assert.That(childCalls, Is.Zero);
            UpdateHolder.Update(1, 1);
            Assert.That(childCalls, Is.EqualTo(1));
        }

        [Test]
        public void DoubleStartAndPhaseChange_DoNotDuplicateOrLoseTween()
        {
            int completions = 0;
            var tween = Tween.DelayedCall(1, () => completions++);
            tween.StartTween().SetUpdateMethod(UpdateMethod.LateUpdate);
            UpdateHolder.Update(1, 1);
            holders[2].Update(1, 1);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(tween.ActiveID, Is.EqualTo(-1));
            Assert.That(Array.Find(UpdateHolder.Tweens, item => ReferenceEquals(item, tween)), Is.Null);
        }

        [Test]
        public void ThrowingCompletion_DoesNotReplayOrStarveOtherTweens()
        {
            int siblingCalls = 0, otherTweenCalls = 0;
            Tween.DelayedCall(0.1f, () => throw new InvalidOperationException("test callback"))
                .OnComplete(() => siblingCalls++);
            Tween.DelayedCall(0.1f, () => otherTweenCalls++);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: test callback");
            UpdateHolder.Update(1, 1);
            UpdateHolder.Update(1, 1);
            Assert.That(siblingCalls, Is.EqualTo(1));
            Assert.That(otherTweenCalls, Is.EqualTo(1));
        }

        [Test]
        public void ForceCompletingCollection_WaitsForAllFinalValuesAndNotifiesOnce()
        {
            var collection = new TweenCaseCollection();
            float first = 0, second = 0;
            var observations = new List<Vector2>();
            collection.AddTween(Tween.DoFloat(0, 1, 10, value => first = value, delay: 2));
            collection.AddTween(Tween.DoFloat(0, 2, 10, value => second = value, delay: 2));
            collection.OnComplete(() => observations.Add(new Vector2(first, second)));
            collection.Complete();
            UpdateHolder.Update(0.1f, 0.1f);
            CollectionAssert.AreEqual(new[] { new Vector2(1, 2) }, observations);
        }

        [Test]
        public void Unload_StopsAndDetachesActiveHandles()
        {
            var tween = Tween.DelayedCall(1, () => Assert.Fail("An unloaded tween must not finish."));
            UpdateHolder.Unload();
            Assert.That(tween.IsActive, Is.False);
            Assert.That(tween.ActiveID, Is.EqualTo(-1));
            Assert.DoesNotThrow(() => tween.Kill());
        }

        [Test]
        public void UnloadInsideCallback_DefersTheNewQueueUntilTheNextTick()
        {
            int calls = 0;
            Tween.DelayedCall(0.1f, () =>
            {
                UpdateHolder.Unload();
                Tween.DelayedCall(0.1f, () => calls++);
            });
            var oldTween = Tween.DelayedCall(0.1f, () => Assert.Fail("The old queue must be discarded."));

            UpdateHolder.Update(1, 1);
            Assert.That(oldTween.IsActive, Is.False);
            Assert.That(calls, Is.Zero);
            UpdateHolder.Update(1, 1);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Collection_ObservesTweensAddedByCompletionCallbacks()
        {
            var collection = new TweenCaseCollection();
            int calls = 0;
            var first = Tween.DelayedCall(0.1f, () => collection.AddTween(Tween.DelayedCall(0.1f, null)));
            collection.AddTween(first);
            collection.OnComplete(() => throw new InvalidOperationException("group callback"));
            collection.OnComplete(() => calls++);

            UpdateHolder.Update(1, 1);
            Assert.That(collection.IsComplete(), Is.False);
            Assert.That(calls, Is.Zero);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: group callback");
            UpdateHolder.Update(1, 1);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ZeroTimeScale_FreezesScaledTweenUntilExplicitCompletion()
        {
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                float scaled = 0, unscaled = 0;
                var scaledTween = Tween.DoFloat(0, 1, 1, value => scaled = value, delay: 1);
                Tween.DoFloat(0, 1, 1, value => unscaled = value, unscaledTime: true);
                UpdateHolder.Update(0, 0.5f);
                Assert.That(scaled, Is.Zero);
                Assert.That(unscaled, Is.EqualTo(0.5f));
                scaledTween.Complete();
                UpdateHolder.Update(0, 0.5f);
                Assert.That(scaled, Is.EqualTo(1));
                Assert.That(unscaled, Is.EqualTo(1));
            }
            finally { Time.timeScale = previousScale; }
        }

        [Test]
        public void ConstantRotation_UsesUnscaledStepAndClipsTheLastTick()
        {
            var target = new GameObject("Rotate test");
            try
            {
                target.transform.DORotateConstant(new Vector3(0, 0, 90), 1, unscaledTime: true);
                UpdateHolder.Update(0, 0.4f);
                Assert.That(target.transform.eulerAngles.z, Is.EqualTo(36).Within(0.01f));
                UpdateHolder.Update(0, 2);
                Assert.That(target.transform.eulerAngles.z, Is.EqualTo(90).Within(0.01f));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void LookAt2D_KeepsFinalRotationInTheXYPlane()
        {
            var target = new GameObject("LookAt2D test");
            try
            {
                target.transform.DOLookAt2D(Vector3.right, TransformTweenCases.LookAt2D.LookAtType.Up, 0.1f);
                UpdateHolder.Update(1, 1);
                Assert.That(Vector3.Angle(target.transform.up, Vector3.right), Is.LessThan(0.01f));
                Assert.That(Vector3.Angle(target.transform.forward, Vector3.forward), Is.LessThan(0.01f));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void DestroyedFollowTarget_CancelsPositionAndBezierTweens()
        {
            var source = new GameObject("Follower");
            var target = new GameObject("Follow target");
            try
            {
                target.transform.position = Vector3.right;
                var move = new TransformTweenCases.PositionTransform(source.transform, target.transform).SetDuration(1).StartTween();
                var bezier = source.transform.DOBezierFollow(target.transform, 1, 0, 0, 1);
                var snapshot = source.transform.DOMove(target.transform, 1);
                Object.DestroyImmediate(target);
                Assert.DoesNotThrow(() => UpdateHolder.Update(0.5f, 0.5f));
                Assert.That(move.IsActive, Is.False);
                Assert.That(bezier.IsActive, Is.False);
                Assert.That(snapshot.IsActive, Is.True);
                Assert.That(source.transform.position.x, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                if (target != null) Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ScrollSnap_AddsTheDeltaAndPreservesTheDisabledAxis()
        {
            var root = new GameObject("Scroll test", typeof(RectTransform), typeof(ScrollRect));
            try
            {
                var scroll = root.GetComponent<ScrollRect>();
                scroll.viewport = root.GetComponent<RectTransform>();
                var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(scroll.viewport, false);
                content.anchoredPosition = new Vector2(40, 50);
                scroll.content = content;
                scroll.horizontal = false;
                scroll.vertical = true;
                TweenExtensions.SnapToTarget(scroll, scroll.viewport.TransformPoint(new Vector3(15, 20)), 0.1f);
                UpdateHolder.Update(1, 1);
                Assert.That(content.anchoredPosition, Is.EqualTo(new Vector2(40, 30)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BounceInOut_IsAvailableAndHasTheExpectedEndpoints()
        {
            Assert.That(Ease.Interpolate(0, Ease.Type.BounceInOut), Is.EqualTo(0).Within(0.0001f));
            Assert.That(Ease.Interpolate(0.5f, Ease.Type.BounceInOut), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(Ease.Interpolate(1, Ease.Type.BounceInOut), Is.EqualTo(1).Within(0.0001f));
        }
    }
}
