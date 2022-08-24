using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FisherYatesShuffle_ScriptableObject<T> : ScriptableObject
{
    [System.Serializable] public class ShuffleGroup : FisherYatesShuffle<T> { }
    public ShuffleGroup fisherYatesShuffle = new ShuffleGroup();

    public virtual void OnEnable()
    {
        fisherYatesShuffle.Reset();
    }

    public T Next() => fisherYatesShuffle.Next();
    public int Shuffle() => fisherYatesShuffle.Shuffle();
    protected virtual bool ItemAllowedInRoll(T item, int index) => true;
    public static implicit operator T(FisherYatesShuffle_ScriptableObject<T> shuffler)
    {
        return shuffler.fisherYatesShuffle.Next();
    }
}
