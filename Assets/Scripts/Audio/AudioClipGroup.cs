using System;

using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Clip Group", menuName = "Random/Audio Clip Group")]
[Serializable]
public class AudioClipGroup : FisherYatesShuffle_ScriptableObject<AudioClip> { }