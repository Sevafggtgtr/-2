using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NetworkAudioSource : NetworkBehaviour
{
    private AudioSource _audioSource;
    public AudioSource AudioSource => _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    [Rpc(SendTo.Server)]
    private void PlayAudioServerRpc(string audioName)
    {
        PlayAudioClientRpc(audioName);
    }

    [Rpc(SendTo.NotMe)]
    private void PlayAudioClientRpc(string audioName)
    {
        _audioSource.PlayOneShot((AudioClip)Resources.Load("Audio/" + audioName));
    }

    public void PlayAudio(AudioClip audioClip)
    {
        _audioSource.PlayOneShot(audioClip);

        PlayAudioServerRpc(audioClip.name);
    }
}
