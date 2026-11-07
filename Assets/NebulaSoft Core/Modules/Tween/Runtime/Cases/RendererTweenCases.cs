using UnityEngine;

namespace NebulaSoft
{
    public static class RendererTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change color of renderer
        /// </summary>
        public static TweenCase DOPropertyBlockColor(this Renderer tweenObject, int colorID, MaterialPropertyBlock materialPropertyBlock, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new PropertyBlockColor(colorID, materialPropertyBlock, tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change float of renderer
        /// </summary>
        public static TweenCase DOPropertyBlockFloat(this Renderer tweenObject, int floatID, MaterialPropertyBlock materialPropertyBlock, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new PropertyBlockFloat(floatID, materialPropertyBlock, tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class PropertyBlockFloat : TweenCaseFunction<Renderer, float>
        {
            private MaterialPropertyBlock materialPropertyBlock;

            private int floatID;

            public PropertyBlockFloat(int floatID, MaterialPropertyBlock materialPropertyBlock, Renderer tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                this.parentObject = tweenObject.gameObject;
                this.materialPropertyBlock = materialPropertyBlock;

                this.floatID = floatID;
                this.startValue = materialPropertyBlock.GetFloat(floatID);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(floatID, resultValue);
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(floatID, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }
        }

        public class PropertyBlockColor : TweenCaseFunction<Renderer, Color>
        {
            private MaterialPropertyBlock materialPropertyBlock;

            private int colorID;

            public PropertyBlockColor(int colorID, MaterialPropertyBlock materialPropertyBlock, Renderer tweenObject, Color resultValue) : base(tweenObject, resultValue)
            {
                this.parentObject = tweenObject.gameObject;
                this.materialPropertyBlock = materialPropertyBlock;

                this.colorID = colorID;
                this.startValue = materialPropertyBlock.GetColor(colorID);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor(colorID, resultValue);
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor(colorID, Color.LerpUnclamped(startValue, resultValue, Interpolate(state)));
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }
        }
    }
}
