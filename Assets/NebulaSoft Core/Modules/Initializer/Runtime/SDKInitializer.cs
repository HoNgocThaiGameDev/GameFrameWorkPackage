using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public class SDKInitializer : MonoBehaviour
    {
        private SDKBehavior[] behaviors;
        private ISDKTaskBehavior[] tasksBehaviors;

        private bool isCompleted;
        private readonly HashSet<SDKBehavior> notified = new HashSet<SDKBehavior>();
        private readonly HashSet<SDKBehavior> startedBehaviors = new HashSet<SDKBehavior>();
        private readonly HashSet<ISDKTaskBehavior> startedTasks = new HashSet<ISDKTaskBehavior>();

        public void Init()
        {
            isCompleted = false;

            behaviors = GetComponents<SDKBehavior>();
            foreach (SDKBehavior behavior in behaviors)
            {
                if (behavior == null || startedBehaviors.Contains(behavior)) continue;
                behavior.Init();
                startedBehaviors.Add(behavior);
            }
            tasksBehaviors = GetComponents<ISDKTaskBehavior>();
            foreach (ISDKTaskBehavior taskBehavior in tasksBehaviors)
            {
                if (taskBehavior == null) continue;
                if (!startedTasks.Contains(taskBehavior) || taskBehavior.Task == null || taskBehavior.Task.Status == LoadingTask.CompleteStatus.Failed ||
                    taskBehavior.Task.Status == LoadingTask.CompleteStatus.TimedOut)
                {
                    taskBehavior.Init(this);
                    startedTasks.Add(taskBehavior);
                }
                GameLoading.AddTask(taskBehavior.Task);
            }
            StopAllCoroutines();
            StartCoroutine(TasksCheck());
        }

        private IEnumerator TasksCheck()
        {
            while (!isCompleted)
            {
                yield return null;

                bool completed = true;
                foreach (ISDKTaskBehavior taskBehavior in tasksBehaviors)
                {
                    if(taskBehavior == null) continue;

                    if (!taskBehavior.Task.IsFinished)
                    {
                        taskBehavior.Task.CheckTimeout(Time.realtimeSinceStartupAsDouble);
                        completed = false;
                    }
                }

                if (completed)
                    OnTasksCompleted();
            }
        }

        private void OnTasksCompleted()
        {
            if (isCompleted) return;

            isCompleted = true;
            foreach (var task in tasksBehaviors)
                if (task.Task.Status == LoadingTask.CompleteStatus.Failed || task.Task.Status == LoadingTask.CompleteStatus.TimedOut) return;
            foreach (SDKBehavior behavior in behaviors)
            {
                if (behavior == null || notified.Contains(behavior)) continue;
                try { behavior.OnUserConsentReceived(); notified.Add(behavior); }
                catch (System.Exception exception) { Debug.LogWarning("[SDK Initializer] " + exception.Message); }
            }
        }

        public void Shutdown()
        {
            StopAllCoroutines();
            if (behaviors != null)
                foreach (var behavior in behaviors)
                    if (behavior != null)
                        try { behavior.Shutdown(); }
                        catch (System.Exception exception) { Debug.LogException(exception); }
            startedBehaviors.Clear();
            startedTasks.Clear();
            notified.Clear();
        }
    }
}
