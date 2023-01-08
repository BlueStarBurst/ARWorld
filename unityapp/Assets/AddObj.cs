using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARSubsystems;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Storage;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class AddObj : PressInputBase
    {
        [SerializeField]
        [Tooltip("Instantiates this prefab on a plane at the touch location.")]
        GameObject m_PlacedPrefab;
        [SerializeField]
        [Tooltip("Instantiates this prefab on a plane at the touch location.")]
        GameObject m_PosterPrefab;

        /// <summary>
        /// The prefab to instantiate on touch.
        /// </summary>
        public GameObject placedPrefab
        {
            get { return m_PlacedPrefab; }
            set { m_PlacedPrefab = value; }
        }

        public GameObject posterPrefab
        {
            get { return m_PosterPrefab; }
            set { m_PosterPrefab = value; }
        }

        /// <summary>
        /// The object instantiated as a result of a successful raycast intersection with a plane.
        /// </summary>
        public GameObject spawnedObject { get; private set; }

        /// <summary>
        /// Invoked whenever an object is placed in on a plane.
        /// </summary>
        public static event Action onPlacedObject;

        ARRaycastManager m_RaycastManager;

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        public bool isAdding = false;

        public string type = "";
        public string user = "";
        public string id = "";

        public string change = "rotate";

        public ARWorldMapController arWorldMapController;
        public GameObject centerChunk = null;
        public GameObject currentChunk = null;

        public API api;

        protected override void Awake()
        {
            base.Awake();
            m_RaycastManager = GetComponent<ARRaycastManager>();
        }

        // protected override void OnPress

        protected override void OnPress(Vector3 position)
        {
            // if (!isAdding)
            // {
            //     Add();
            //     return;
            // }

        }

        public void CreateObjectInFrontOfCamera(string type, string user, string id)
        {
            if (!isAdding)
            {

                if (arWorldMapController.centerChunk == null)
                {
                    return;
                }

                if (centerChunk == null || centerChunk != arWorldMapController.centerChunk)
                {
                    centerChunk = arWorldMapController.centerChunk;
                }

                this.type = type;
                this.user = user;
                this.id = id;

                if (type.Equals("posters") || type.Equals("stickers") || type.Equals("images"))
                {
                    HostNativeAPI.addingObj("adding"); // not adding
                    AddPoster(type, user, id);
                }
                return;
            }
        }

        async void AddPoster(string type, string user, string id)
        {
            isAdding = true;
            Debug.Log("Adding: users/" + user + "/posters/" + id + ".jpg");
            Debug.Log("type: " + type);


            // raycast directly in front of camera to place object 0.5 units above plane hit relative to plane normal. If there is no plane hit, place object 0.5 units above camera
            if (m_RaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), s_Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = s_Hits[0].pose;

                // the rotation of the object is relative to the plane normal

                Console.WriteLine("PLANE HIT");

                if (type.Equals("posters") || type.Equals("stickers") || type.Equals("images"))
                {
                    Console.WriteLine("POSTER");

                    // create poster prefab parallel to plane hit normal and 0.5 units above plane hit
                    spawnedObject = Instantiate(m_PosterPrefab, hitPose.position + hitPose.rotation * Vector3.up * 0.1f, hitPose.rotation);
                    // get poster image
                    StorageReference storageRef = FirebaseStorage.GetInstance(FirebaseApp.DefaultInstance).GetReferenceFromUrl("gs://ourworld-737cd.appspot.com");

                    await storageRef.Root.Child("users/" + user + "/" + type + "/" + id + ".jpg").GetDownloadUrlAsync().ContinueWithOnMainThread(async task2 =>
                    {
                        if (task2.IsFaulted || task2.IsCanceled)
                        {
                            Console.WriteLine("FAULTED PNG");

                            byte[] data = await storageRef.Child("users/" + user + "/" + type + "/" + id + ".png").GetBytesAsync(1024 * 1024);
                            Console.WriteLine("Data");
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            spawnedObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                            spawnedObject.transform.localScale = new Vector3(texture.width / 1024f, 1, texture.height / 1024f);
                        }
                        else
                        {
                            Console.WriteLine("WORKING JPG");

                            byte[] data = await storageRef.Child("users/" + user + "/" + type + "/" + id + ".jpg").GetBytesAsync(1024 * 1024);
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            spawnedObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                            spawnedObject.transform.localScale = new Vector3(texture.width / 1024f, 1, texture.height / 1024f);
                        }
                    });


                }
                else
                {
                    spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position + hitPose.rotation * Vector3.up * 0.1f, transform.rotation * Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + 180, 0));
                }

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }
            else
            {
                Console.WriteLine("NOT HIT");
                if (type.Equals("posters") || type.Equals("stickers") || type.Equals("images"))
                {
                    spawnedObject = Instantiate(m_PosterPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, transform.rotation * Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + 180, 0));
                    // get poster image
                    StorageReference storageRef = FirebaseStorage.GetInstance(FirebaseApp.DefaultInstance).GetReferenceFromUrl("gs://ourworld-737cd.appspot.com");

                    await storageRef.Root.Child("users/" + user + "/" + type + "/" + id + ".jpg").GetDownloadUrlAsync().ContinueWithOnMainThread(async task2 =>
                    {
                        if (task2.IsFaulted || task2.IsCanceled)
                        {
                            Console.WriteLine("FAULTED PNG");

                            byte[] data = await storageRef.Child("users/" + user + "/" + type + "/" + id + ".png").GetBytesAsync(1024 * 1024);
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            spawnedObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                            spawnedObject.transform.localScale = new Vector3(texture.width / 1024f, 1, texture.height / 1024f);
                        }
                        else
                        {
                            Console.WriteLine("WORKING JPG");

                            byte[] data = await storageRef.Child("users/" + user + "/" + type + "/" + id + ".jpg").GetBytesAsync(1024 * 1024);
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            spawnedObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                            spawnedObject.transform.localScale = new Vector3(texture.width / 1024f, 1, texture.height / 1024f);
                        }
                    });
                }
                else
                {
                    spawnedObject = Instantiate(m_PlacedPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, transform.rotation * Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + 180, 0));
                }

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }

            // get distance between spawned object and center chunk
            float distanceToCenterChunk = Vector3.Distance(spawnedObject.transform.position, centerChunk.transform.position);

            // get components of distance to center chunk in the direction of center chunk forward
            float distanceToCenterChunkForward = Vector3.Dot(spawnedObject.transform.position - centerChunk.transform.position, centerChunk.transform.forward);

            // get components of distance to center chunk in the direction of center chunk right
            float distanceToCenterChunkRight = Vector3.Dot(spawnedObject.transform.position - centerChunk.transform.position, centerChunk.transform.right);

            // round down to nearest 1 unit
            int roundedDistanceToCenterChunkForward = (int)Math.Round(distanceToCenterChunkForward);
            int roundedDistanceToCenterChunkRight = (int)Math.Round(distanceToCenterChunkRight);

            // convert to coordinates relative to the world map

            Vector3 centerChunkCoordinates = centerChunk.transform.position;

            if (currentChunk == null)
            {
                currentChunk = Instantiate(arWorldMapController.ChunkPrefab, centerChunkCoordinates + centerChunk.transform.forward * roundedDistanceToCenterChunkForward + centerChunk.transform.right * roundedDistanceToCenterChunkRight, centerChunk.transform.rotation);
            }
            else
            {
                currentChunk.transform.position = centerChunkCoordinates + centerChunk.transform.forward * roundedDistanceToCenterChunkForward + centerChunk.transform.right * roundedDistanceToCenterChunkRight;
                currentChunk.transform.rotation = centerChunk.transform.rotation;
            }
        }

        private Vector3 previousPosition = Vector3.zero;
        private Quaternion trueRot = Quaternion.identity;
        private bool rotating = false;
        private Quaternion previousRotation = Quaternion.identity;
        private Quaternion newRotation = Quaternion.identity;
        private void Update()
        {

            if (Input.touchCount < 1 && !Input.GetMouseButton(0))
            {
                rotating = false;
                return;
            }

            // get position of touch

            Vector3 position = Vector3.zero;
            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
            }
            else if (Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
            }

            position = Input.GetTouch(0).position;


            if (change.Equals("move"))
            {

                // if touch is on screen
                if (position.x > 0 && position.x < Screen.width && position.y > 0 && position.y < Screen.height)
                {

                    if (arWorldMapController.centerChunk == null)
                    {
                        return;
                    }

                    if (centerChunk == null || centerChunk != arWorldMapController.centerChunk)
                    {
                        centerChunk = arWorldMapController.centerChunk;
                    }

                    if (spawnedObject == null)
                    {
                        return;
                    }
                    if (m_RaycastManager.Raycast(position, s_Hits, TrackableType.PlaneWithinPolygon))
                    {
                        Pose hitPose = s_Hits[0].pose;

                        // spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);

                        // get axis of the plane and print it out
                        Vector3 planeNormal = hitPose.rotation * Vector3.up;

                        // // get if object is behind plane
                        // bool isBehindPlane = Vector3.Dot(spawnedObject.transform.position - hitPose.position, planeNormal) < 0;

                        // get distance between object and plane only in the direction of the plane normal
                        float distance = Vector3.Dot(spawnedObject.transform.position - hitPose.position, planeNormal);

                        // move object to the plane


                        // if poster, make the rotation same as the plane normal
                        if (type.Equals("posters") || type.Equals("stickers") || type.Equals("images"))
                        {
                            Console.WriteLine("POSTER");
                            spawnedObject.transform.rotation = hitPose.rotation;
                            spawnedObject.transform.position = hitPose.position + hitPose.rotation * Vector3.up * 0.1f;
                        }
                        else
                        {
                            spawnedObject.transform.position = hitPose.position + hitPose.rotation * Vector3.up * Math.Max(distance, 0.1f);
                        }

                    }
                    else
                    {
                        // if no plane is hit, move object to 0.5 units in front of camera at position of touch
                        Vector3 touchPosition = Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, 1.5f));
                        spawnedObject.transform.position = touchPosition;
                    }


                    // get distance between spawned object and center chunk
                    float distanceToCenterChunk = Vector3.Distance(spawnedObject.transform.position, centerChunk.transform.position);

                    // get components of distance to center chunk in the direction of center chunk forward
                    float distanceToCenterChunkForward = Vector3.Dot(spawnedObject.transform.position - centerChunk.transform.position, centerChunk.transform.forward);

                    // get components of distance to center chunk in the direction of center chunk right
                    float distanceToCenterChunkRight = Vector3.Dot(spawnedObject.transform.position - centerChunk.transform.position, centerChunk.transform.right);

                    // round down to nearest 1 unit
                    int roundedDistanceToCenterChunkForward = (int)Math.Round(distanceToCenterChunkForward);
                    int roundedDistanceToCenterChunkRight = (int)Math.Round(distanceToCenterChunkRight);

                    // convert to coordinates relative to the world map

                    Vector3 centerChunkCoordinates = centerChunk.transform.position;

                    if (currentChunk == null)
                    {
                        currentChunk = Instantiate(arWorldMapController.ChunkPrefab, centerChunkCoordinates + centerChunk.transform.forward * roundedDistanceToCenterChunkForward + centerChunk.transform.right * roundedDistanceToCenterChunkRight, centerChunk.transform.rotation);
                    }
                    else
                    {
                        currentChunk.transform.position = centerChunkCoordinates + centerChunk.transform.forward * roundedDistanceToCenterChunkForward + centerChunk.transform.right * roundedDistanceToCenterChunkRight;
                        currentChunk.transform.rotation = centerChunk.transform.rotation;
                    }

                }

            }
            else if (change.Equals("rotate"))
            {
                if (rotating == false)
                {
                    rotating = true;
                    trueRot = spawnedObject.transform.rotation;
                }

                // // rotate object relative to camera position using delta position of touch
                // if (spawnedObject != null)
                // {
                //     Vector2 delta = Input.GetTouch(0).deltaPosition;

                //     Vector3 right = Vector3.Cross(Camera.main.transform.up, spawnedObject.transform.position - Camera.main.transform.position);

                //     Vector3 up = Vector3.Cross(spawnedObject.transform.position - Camera.main.transform.position, right);

                //     trueRot = Quaternion.AngleAxis(-delta.x * 0.1f, up) * trueRot;

                //     trueRot = Quaternion.AngleAxis(delta.y * 0.1f, right) * trueRot;

                //     // snap to closest 15 degrees when rotating object with trueRot
                //     Vector3 eulerRot = trueRot.eulerAngles;
                //     Vector3 roundedRot = new Vector3(Mathf.Round(eulerRot.x / 15) * 15, Mathf.Round(eulerRot.y / 15) * 15, Mathf.Round(eulerRot.z / 15) * 15);

                //     // rotate object
                //     spawnedObject.transform.rotation = Quaternion.Euler(roundedRot);

                // }


                if (spawnedObject != null)
                {
                    Vector2 delta = Input.GetTouch(0).deltaPosition;

                    // swipe delta x rotates about y axis of object
                    // spawnedObject.transform.Rotate(Vector3.up, -delta.x * 0.1f, Space.World);

                    // swipe delta y rotates about x axis relative to world
                    trueRot = Quaternion.AngleAxis(delta.y * 0.1f, Vector3.right) * trueRot;

                    // snap to closest 15 degrees when rotating object with trueRot
                    Vector3 eulerRot = trueRot.eulerAngles;
                    Vector3 roundedRot = new Vector3(Mathf.Round(eulerRot.x / 15) * 15, Mathf.Round(eulerRot.y / 15) * 15, Mathf.Round(eulerRot.z / 15) * 15);

                    // rotate object
                    spawnedObject.transform.rotation = Quaternion.Euler(roundedRot);


                }

                // // rotate object relative to camera position using delta position of touch
                // if (spawnedObject != null) {
                //     Vector2 delta = Input.GetTouch(0).deltaPosition;
                //     Vector3 right = Vector3.Cross(Camera.main.transform.up, spawnedObject.transform.position - Camera.main.transform.position);
                //     Vector3 up = Vector3.Cross(spawnedObject.transform.position - Camera.main.transform.position, right);

                //     // snap to closest 15 degrees when rotating object
                //     float angle = Vector3.Angle(spawnedObject.transform.forward, Camera.main.transform.forward);
                //     float angleToSnap = Mathf.Round(angle / 15) * 15;
                //     float angleDifference = angleToSnap - angle;

                //     // rotate object
                //     spawnedObject.transform.rotation = Quaternion.AngleAxis(-delta.x * 0.25f, up) * spawnedObject.transform.rotation;
                //     spawnedObject.transform.rotation = Quaternion.AngleAxis(delta.y * 0.25f, right) * spawnedObject.transform.rotation;

                // }

            }


        }
    }
}
