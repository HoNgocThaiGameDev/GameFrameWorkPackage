using System.Collections.Generic;

namespace NebulaSoft
{
    public class TweenCaseCollection
    {
        private List<TweenCase> tweenCases = new List<TweenCase>();
        public List<TweenCase> TweenCases => tweenCases;

        private SimpleCallback tweensCompleted;

        public void AddTween(TweenCase tweenCase)
        {
            if (tweenCase == null || tweenCases.Contains(tweenCase)) return;
            if (!tweenCase.HasInvokedCompletion && !tweenCase.IsKilling)
                tweenCase.CompletionDelivered += OnTweenCaseComplete;

            tweenCases.Add(tweenCase);
        }

        public bool IsComplete()
        {
            for(int i = 0; i < tweenCases.Count; i++)
            {
                if (!tweenCases[i].HasInvokedCompletion)
                    return false;
            }

            return true;
        }

        public void Complete()
        {
            for (int i = 0; i < tweenCases.Count; i++)
            {
                tweenCases[i].Complete();
            }
        }

        public void Kill()
        {
            for (int i = 0; i < tweenCases.Count; i++)
            {
                tweenCases[i].Kill();
            }
        }

        public void OnComplete(SimpleCallback callback)
        {
            tweensCompleted += callback;
        }

        private void OnTweenCaseComplete()
        {
            if (!IsComplete()) return;
            var callbacks = tweensCompleted;
            tweensCompleted = null;
            TweenCase.InvokeSubscribers(callbacks);
        }

        public static TweenCaseCollection operator +(TweenCaseCollection caseCollection, TweenCase tweenCase)
        {
            if(caseCollection == null)
                caseCollection = new TweenCaseCollection();

            caseCollection.AddTween(tweenCase);

            return caseCollection;
        }
    }
}
