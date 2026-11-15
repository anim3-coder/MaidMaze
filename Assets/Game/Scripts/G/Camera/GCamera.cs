using System.Collections;
using TriInspector;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class GCamera : MonoBehaviour
{
    [SerializeField, RequiredGet(IncludeSelf = true)] private CinemachineCamera camera; 
    private void OnEnable()
    {
        Player.OnCreateLocalBody += OnStartLocalPlayer;
    }
    private void OnStartLocalPlayer(Body body)
    {
        camera.Follow = body.transform;
    }
    private void OnDisable()
    {
        Player.OnCreateLocalBody -= OnStartLocalPlayer;
    }
}