using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class WeightedList<T> where T : class
    {
        [SerializeField] List<WeightedItem<T>> items = new List<WeightedItem<T>>();
        public List<WeightedItem<T>> Items => items;

        public float TotalWeight => items.Sum(x => x.Weight);

        public WeightedList()
        {

        }

        public WeightedList(List<WeightedItem<T>> items)
        {
            this.items = items;
        }

        public void Add(WeightedItem<T> item)
        {
            items.Add(item);
        }

        public T GetRandomItem()
        {
            if (items.IsNullOrEmpty())
            {
                Debug.LogError("Weighted list can't be empty");
                return null;
            }

            if (TotalWeight == 0f)
            {
                Debug.LogWarning("All weights are 0. Returning element with index 0.");
                return items[0].Item;
            }

            float randomValue = Random.Range(0f, TotalWeight);
            float currentWeight = 0f;

            foreach (WeightedItem<T> item in items)
            {
                currentWeight += item.Weight;

                if (currentWeight >= randomValue)
                {
                    return item.Item;
                }
            }

            // probably impossible case, but we have to return something just in case
            return items[0].Item;
        }
    }

    [System.Serializable]
    public class WeightedItem<I>
    {
        [SerializeField] float weight = 1f;
        public float Weight => weight;

        [SerializeField] I item;
        public I Item => item;

        public WeightedItem(float weight, I item)
        {
            this.weight = weight;
            this.item = item;
        }
    }
}