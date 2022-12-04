using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEngine;
using AOT;
using Newtonsoft.Json;
using Unity.Collections;
using Firebase;
using Firebase.Auth;


/// <summary>
/// C-API exposed by the Host, i.e., Unity -> Host API.
/// </summary>
public class HostNativeAPI
{
    public delegate void TestDelegate(string name);

    [DllImport("__Internal")]
    public static extern void sendUnityStateUpdate(string state);

    [DllImport("__Internal")]
    public static extern void setTestDelegate(TestDelegate cb);

    // // byte array marshalling: https://stackoverflow.com/questions/10010873/how-to-pass-byte-array-to-c-sharp-dll
    // [DllImport("__Internal")]
    // public static extern void saveARWorldMap(byte[] data, int length);

    [DllImport("__Internal")]
    public static extern void saveMap(string map);

    [DllImport("__Internal")]
    public static extern void setPhoneResponse(string response);
}

/// <summary>
/// C-API exposed by Unity, i.e., Host -> Unity API.
/// </summary>
public class UnityNativeAPI
{

    [MonoPInvokeCallback(typeof(HostNativeAPI.TestDelegate))]
    public static void test(string name)
    {
        Debug.Log("This static function has been called from iOS!");
        Debug.Log(name);
    }

}

/// <summary>
/// This structure holds the type of an incoming message.
/// Based on the type, we will parse the extra provided data.
/// </summary>
public struct Message
{
    public string type;
}

/// <summary>
/// This structure holds the type of an incoming message, as well
/// as some data.
/// </summary>
public struct MessageWithData<T>
{
    [JsonProperty(Required = Newtonsoft.Json.Required.AllowNull)]
    public string type;

    [JsonProperty(Required = Newtonsoft.Json.Required.AllowNull)]
    public T data;
}

public class API : MonoBehaviour
{
    public GameObject cube;
    public ARWorldMapController worldMapController;

    void Start()
    {
#if UNITY_IOS
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            HostNativeAPI.setTestDelegate(UnityNativeAPI.test);
            HostNativeAPI.sendUnityStateUpdate("ready");
        }
#endif
    }

    void ReceiveMessage(string serializedMessage)
    {
        var header = JsonConvert.DeserializeObject<Message>(serializedMessage);
        switch (header.type)
        {
            case "change-color":
                _UpdateCubeColor(serializedMessage);
                break;
            case "phone-login":
                _PhoneLogin(serializedMessage);
                break;
            case "save-map":
                _SaveMap(serializedMessage);
                // HostNativeAPI.saveMap("hehehe");
                break;
            case "load-map":
                _LoadMap(serializedMessage);
                break;
            default:
                Debug.LogError("Unrecognized message '" + header.type + "'");
                break;
        }
    }

    public void _PhoneLogin(string serialized)
    {
        var msg = JsonConvert.DeserializeObject<MessageWithData<string[]>>(serialized);
        // firebase 
        var countryCode = msg.data[0];
        var phoneNumber = msg.data[1];

        var FirebaseAuth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        PhoneAuthProvider provider = PhoneAuthProvider.GetInstance(FirebaseAuth);
        provider.VerifyPhoneNumber(phoneNumber, 300000, null,
        verificationCompleted: (credential) =>
        {
            // Auto-sms-retrieval or instant validation has succeeded (Android only).
            // There is no need to input the verification code.
            // `credential` can be used instead of calling GetCredential().
        },
        verificationFailed: (error) =>
        {
            // The verification code was not sent.
            // `error` contains a human readable explanation of the problem.
            HostNativeAPI.setPhoneResponse("error");
        },
        codeSent: (id, token) =>
        {
            HostNativeAPI.setPhoneResponse("verify");
            // Verification code was successfully sent via SMS.
            // `id` contains the verification id that will need to passed in with
            // the code from the user when calling GetCredential().
            // `token` can be used if the user requests the code be sent again, to
            // tie the two requests together.
        });
    }

    public void sendMapIOS(string map)
    {
        HostNativeAPI.saveMap("hehehe");
        HostNativeAPI.saveMap(map);
    }

    private void _UpdateCubeColor(string serialized)
    {
        if (cube == null)
        {
            Debug.LogError("Cube is null");
            return;
        }
        var msg = JsonConvert.DeserializeObject<MessageWithData<float[]>>(serialized);
        if (msg.data != null && msg.data.Length >= 3)
        {
            var color = new Color(msg.data[0], msg.data[1], msg.data[2]);
            Debug.Log("Setting Color = " + color);
            var material = cube.GetComponent<MeshRenderer>()?.sharedMaterial;
            material?.SetColor("_Color", color);
        }
    }

    private void _SaveMap(string serialized)
    {
        if (worldMapController == null)
        {
            Debug.LogError("WorldMapController is null");
            return;
        }
        var msg = JsonConvert.DeserializeObject<MessageWithData<bool>>(serialized);
        if (msg.data == true)
        {
            Debug.Log("Saving Map = " + msg.data);
            worldMapController.OnSaveButton();
        }
    }

    private void _LoadMap(string serialized)
    {
        if (worldMapController == null)
        {
            Debug.LogError("WorldMapController is null");
            return;
        }
        var msg = JsonConvert.DeserializeObject<MessageWithData<bool>>(serialized);
        if (msg.data == true)
        {
            Debug.Log("Loading Map = " + msg.data);
            worldMapController.OnLoadButton();
        }
    }
}
