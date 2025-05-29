using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public abstract class MiniMaxValue<T>
    {
        [SerializeField] protected T _minValue;
        [SerializeField] protected T _maxValue;

        public T MinValue => _minValue;
        public T MaxValue => _maxValue;

        public abstract T GetRandom();
    }

    [Serializable]
    public class IntMiniMaxValue : MiniMaxValue<int>
    {
        public override int GetRandom()
        {
            return UnityEngine.Random.Range(_minValue, _maxValue);
        }
    }

    [Serializable]
    public class FloatMiniMaxValue : MiniMaxValue<float>
    {
        public override float GetRandom()
        {
            return UnityEngine.Random.Range(_minValue, _maxValue);
        }
    }
}