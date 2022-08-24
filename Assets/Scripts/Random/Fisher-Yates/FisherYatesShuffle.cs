using UnityEngine;

[System.Serializable]
public abstract class FisherYatesShuffle
{
    public static int Shuffle<T>(ref T[] array)
    {
        int len = array.Length;
        for (int i = array.Length - 1; i > 0; i--)
        {
            // Randomize a number between 0 and i (so that the range decreases each time)
            int rnd = Random.Range(0, i);

            // Save the value of the current i, otherwise it'll overright when we swap the values
            T temp = array[i];

            // Swap the new and old values
            array[i] = array[rnd];
            array[rnd] = temp;
        }

        return len;
    }
}

[System.Serializable]
public class FisherYatesShuffle<T> : FisherYatesShuffle
{
    public T[] sequence;
    public int length { get; private set; } = 0;
    public int nextIndex { get; private set; } = 0;

    public FisherYatesShuffle(T[] sequence)
    {
        this.sequence = sequence;
        Shuffle();
    }
    public FisherYatesShuffle()
    {
        this.sequence = new T[0];
    }

    /// <summary>
    /// Ensures the next roll will shuffle the sequence.
    /// </summary>
    public void Reset()
    {
        length = 0;
        nextIndex = 0;
    }

    public int Shuffle()
    {
        length = FisherYatesShuffle.Shuffle<T>(ref sequence);
        nextIndex = 0;

        return length;
    }

    public T Next()
    {
        if (nextIndex >= length)
            Shuffle();

        T item = sequence[nextIndex];
        nextIndex++;

        return item;
    }
}
