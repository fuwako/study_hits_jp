
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)] // Requirement of VRC Object Sync.
public class TextureIterator : UdonSharpBehaviour
{
    private VRCPlayerApi _localPlayer;

    [Header("【説明】")]
    [Space(-10)]
    [Header("このゲームオブジェクトは持って使われれば、次のテクスチャを見せます。")]
    [Space(-10)]
    [Header("「TextureIterator Example」のプレハブをどうぞ調べてください。")]

    [Header("下にある「Textures」にはテクスチャを入れてください。")]
    
    [Space(10)]
    [Header("【依存関係】")]
    [Space(-10)]
    [Header("•　コンポネントのMesh Renderer")]
    [Space(-10)]
    [Header("•　Mesh Rendererにはマテリアルがあります。")]
    [Space(-10)]
    [Header("•　コンポネントのVRC Pickup")]
    [Space(-10)]
    [Header("•　VRC Pickupでは「AutoHold」が「Yes」を設定しなければ使えません。")]

    [Space(10)]
    [Header("【設定】")]
    [Tooltip("無効ならローカルです。有効ならグローバルです。")]
    [SerializeField] private bool _sync = true;
    [Tooltip("ここにはテクスチャを入れてください。Element 0にマテリアルの現在のmain texture（BaseやAlbedo）があります。変更すると、マテリアルも変更されます。")]
    [SerializeField] private Texture[] _textures;

    [Space(-10)]
    [Header("【詳細設定】")]
    [SerializeField] private string _property = "_MainTex";

    private Material _mat;
    [UdonSynced] private int _currentTexture = 0;

#if UNITY_EDITOR
    void OnValidate()
    {
        _mat = GetComponent<Renderer>().sharedMaterial;

        if (_textures.Length < 1) // The array is unpopulated.
        {
            _textures = new Texture[1];
            _textures[0] = _mat.GetTexture(_property); // Set the first texture to the material's current main texture.
        }
        else // The array is populated.
        {
            // Match the material's main texture with the first texture.
            // This allows the user to easily modify the texture without viewing the material.
            //
            // Warning: This does not check if the material has a different texture.
            // If the user modifies the material instead of this script's array, they may not understand that the script replaces the material's current texture.
            _mat.SetTexture(_property, _textures[0]);
        }
    }
#endif

    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
        _mat = GetComponent<Renderer>().material;
    }

    public override void OnPickup()
    {
        if (_sync)
        {
            Networking.SetOwner(_localPlayer, gameObject);
        }
    }

    public override void OnPickupUseUp() // When the player is already holding the object and clicks/pulls their trigger.
    {
        if (_sync)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "DisplayNextTexture");
        }
        else
        {
            DisplayNextTexture();
        }
    }

    public void DisplayNextTexture()
    {
        _currentTexture = (_currentTexture + 1) % _textures.Length;

        if (_sync)
        {
            RequestSerialization(); // Update UdonSynced variables for the network.
        }

        _mat.SetTexture(_property, _textures[_currentTexture]);
    }

    public override void OnDeserialization() // Occurs after RequestSerialization() for players that do not own this object. Occurs after OnPlayerJoined() for the late joiner.
    {
        if (_sync)
        {
            _mat.SetTexture(_property, _textures[_currentTexture]);
        }
    }
}
