using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Storage;
using UnityEngine.UI;
using System;
using Firebase.Extensions;
using Siccity.GLTFUtility;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace UnityEngine.XR.ARFoundation.Samples
{

    public class Chunk : MonoBehaviour
    {

        public GameObject ARCamera;
        public string id;
        public ARWorldMapController arWorldMapController;
        public bool isLoaded = false;

        public int cx = 0;
        public int cy = 0;

        public FirebaseFirestore db;

        public bool showMat = false;

        //dictionary of all the elements in the chunk
        public Dictionary<string, GameObject> elements = new Dictionary<string, GameObject>();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (ARCamera == null || arWorldMapController == null || id == null)
            {
                return;
            }

            // if arWorldMapController.showChunks is false, hide chunk material
            if (!showMat && arWorldMapController.showChunks)
            {
                // go through children and turn disable the renderers
                foreach (Transform child in transform)
                {
                    child.GetComponent<MeshRenderer>().enabled = true;
                }
                showMat = true;
            }
            else if (showMat && !arWorldMapController.showChunks)
            {
                // go through children and turn enable the renderers
                foreach (Transform child in transform)
                {
                    child.GetComponent<MeshRenderer>().enabled = false;
                }
                showMat = false;
            }

            // get distance between camera and chunk
            float distance = Vector3.Distance(ARCamera.transform.position, transform.position);
            // if distance is greater than 100, destroy chunk
            if (!isLoaded && distance < 10)
            {
                preFilePath = $"{Application.persistentDataPath}/Files";
                arWorldMapController.Log("Loading chunk " + id);
                try
                {
                    LoadChunk();
                }
                catch (System.Exception e)
                {
                    arWorldMapController.Log("Error loading chunk " + id + ": " + e.Message);
                }
                isLoaded = true;
            }

            // get raycast from touch
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    Ray ray = Camera.main.ScreenPointToRay(touch.position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        // if hit is a poster, open poster
                        if (hit.transform.gameObject.tag == "element")
                        {
                            arWorldMapController.Log("Hit poster " + hit.transform.gameObject.name);
                            
                            // HostNativeAPI.ElementOptions(hit.transform.gameObject.name);
                        }
                    }
                }
            }

        }

        async void LoadChunk()
        {
            // get chunk from firestore
            //id is anchor name
            DocumentReference docRef = db.Collection("maps").Document(arWorldMapController.worldMapId).Collection("chunks").Document(id);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            cx = Convert.ToInt32(snapshot.GetValue<double>("cx"));
            cy = Convert.ToInt32(snapshot.GetValue<double>("cy"));
            int[] chunkPos = new int[] { cx, cy };
            arWorldMapController.chunksPos[cx + "-" + cy] = id;

            if (snapshot.Exists)
            {
                // get chunk data
                Dictionary<string, object> chunkData = snapshot.ToDictionary();

                // get chunk data
                arWorldMapController.Log("Loading chunk " + chunkData["updated"]);

                // get posters from chunk
                CollectionReference postersRef = db.Collection("maps").Document(arWorldMapController.worldMapId).Collection("chunks").Document(id).Collection("posters");
                QuerySnapshot postersSnapshot = await postersRef.GetSnapshotAsync();
                // go through posters and add them to the chunk
                foreach (DocumentSnapshot posterSnapshot in postersSnapshot.Documents)
                {
                    Dictionary<string, object> posterData = posterSnapshot.ToDictionary();
                    arWorldMapController.Log("Loading poster " + posterData["id"] + " from chunk " + id);
                    // create poster
                    GameObject poster = Instantiate(arWorldMapController.posterPrefab, transform);

                    // NOT WORKING :(


                    float x = Convert.ToSingle(posterSnapshot.GetValue<double>("x"));
                    float y = Convert.ToSingle(posterSnapshot.GetValue<double>("y"));
                    float z = Convert.ToSingle(posterSnapshot.GetValue<double>("z"));

                    poster.transform.localPosition = new Vector3(x, y, z);

                    float rx = Convert.ToSingle(posterSnapshot.GetValue<double>("rx"));
                    float ry = Convert.ToSingle(posterSnapshot.GetValue<double>("ry"));
                    float rz = Convert.ToSingle(posterSnapshot.GetValue<double>("rz"));

                    poster.transform.localRotation = Quaternion.Euler(rx, ry, rz);

                    float sx = Convert.ToSingle(posterSnapshot.GetValue<double>("sx"));
                    float sy = Convert.ToSingle(posterSnapshot.GetValue<double>("sy"));
                    float sz = Convert.ToSingle(posterSnapshot.GetValue<double>("sz"));

                    poster.transform.localScale = new Vector3(sx, sy, sz);

                    arWorldMapController.Log("Loading poster users/" + posterData["user"] + "/posters/" + posterData["id"] + ".jpg");



                    // get poster image
                    StorageReference storageRef = FirebaseStorage.GetInstance(FirebaseApp.DefaultInstance).GetReferenceFromUrl("gs://ourworld-737cd.appspot.com");
                    // get image data
                    // byte[] data = await storageRef.Child("users/" + posterData["user"] + "/posters/" + posterData["id"] + ".png").GetBytesAsync(1024 * 1024);

                    await storageRef.Root.Child("users/" + posterData["user"] + "/" + posterData["type"] + "/" + posterData["id"] + ".jpg").GetDownloadUrlAsync().ContinueWithOnMainThread(async task2 =>
                    {
                        if (task2.IsFaulted || task2.IsCanceled)
                        {
                            Console.WriteLine("FAULTED PNG");

                            byte[] data = await storageRef.Child("users/" + posterData["user"] + "/" + posterData["type"] + "/" + posterData["id"] + ".png").GetBytesAsync(1024 * 1024);
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            poster.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                        }
                        else
                        {
                            Console.WriteLine("WORKING JPG");

                            byte[] data = await storageRef.Child("users/" + posterData["user"] + "/" + posterData["type"] + "/" + posterData["id"] + ".jpg").GetBytesAsync(1024 * 1024);
                            // create texture
                            Texture2D texture = new Texture2D(1, 1);
                            // load texture
                            texture.LoadImage(data);
                            // set diffuse texture
                            poster.GetComponent<MeshRenderer>().material.mainTexture = texture;
                            // get width and height of image and set scale of poster
                        }
                    });
                    poster.name = posterSnapshot.Id;
                    poster.tag = "element";

                    elements.Add(posterSnapshot.Id, poster);


                }


                // get posters from chunk
                CollectionReference objectsRef = db.Collection("maps").Document(arWorldMapController.worldMapId).Collection("chunks").Document(id).Collection("objects");
                QuerySnapshot objectsSnapshot = await objectsRef.GetSnapshotAsync();
                // go through posters and add them to the chunk
                foreach (DocumentSnapshot objectSnapshot in objectsSnapshot.Documents)
                {
                    Dictionary<string, object> posterData = objectSnapshot.ToDictionary();
                    arWorldMapController.Log("Loading object " + posterData["id"] + " from chunk " + id);
                    // create poster

                    arWorldMapController.Log("getting locations");

                    float x = Convert.ToSingle(objectSnapshot.GetValue<double>("x"));
                    float y = Convert.ToSingle(objectSnapshot.GetValue<double>("y"));
                    float z = Convert.ToSingle(objectSnapshot.GetValue<double>("z"));


                    float rx = Convert.ToSingle(objectSnapshot.GetValue<double>("rx"));
                    float ry = Convert.ToSingle(objectSnapshot.GetValue<double>("ry"));
                    float rz = Convert.ToSingle(objectSnapshot.GetValue<double>("rz"));


                    float sx = Convert.ToSingle(objectSnapshot.GetValue<double>("sx"));
                    float sy = Convert.ToSingle(objectSnapshot.GetValue<double>("sy"));
                    float sz = Convert.ToSingle(objectSnapshot.GetValue<double>("sz"));

                    arWorldMapController.Log("getting storage ref");

                    // get poster image
                    StorageReference storageRef = FirebaseStorage.GetInstance(FirebaseApp.DefaultInstance).GetReferenceFromUrl("gs://ourworld-737cd.appspot.com");
                    // get image data
                    // byte[] data = await storageRef.Child("users/" + posterData["user"] + "/posters/" + posterData["id"] + ".png").GetBytesAsync(1024 * 1024);
                    arWorldMapController.Log("URL");

                    string url = "users/" + posterData["user"] + "/" + posterData["type"] + "/" + posterData["id"] + ".glb";

                    arWorldMapController.Log("exists");

                    if (File.Exists(preFilePath + url))
                    {
                        // File.Delete(preFilePath + url);
                        LoadModel(preFilePath + url, x, y, z, rx, ry, rz, sx, sy, sz, objectSnapshot.Id);
                        continue;
                    }

                    arWorldMapController.Log("creating new file");

                    // get glb file and instantiate object
                    // await storageRef.Child("users/" + user + "/" + type + "/" + id + ".glb").GetFileAsync(preFilePath + url);
                    await storageRef.Child("users/" + posterData["user"] + "/" + posterData["type"] + "/" + posterData["id"] + ".glb").GetDownloadUrlAsync().ContinueWith((Task<Uri> task) =>
                    {
                        if (!task.IsFaulted && !task.IsCanceled)
                        {
                            arWorldMapController.Log("WORKING GLB");
                            arWorldMapController.Log(task.Result.ToString());
                            DownloadFile(task.Result.ToString(), preFilePath + url, x, y, z, rx, ry, rz, sx, sy, sz, objectSnapshot.Id);
                        }
                        else
                        {
                            arWorldMapController.Log(task.Exception.ToString());
                        }
                    });





                }


                // get posters from chunk
                CollectionReference spotlightsRef = db.Collection("maps").Document(arWorldMapController.worldMapId).Collection("chunks").Document(id).Collection("spotlights");
                QuerySnapshot spotlightsSnapshot = await spotlightsRef.GetSnapshotAsync();
                // go through posters and add them to the chunk
                foreach (DocumentSnapshot posterSnapshot in spotlightsSnapshot.Documents)
                {
                    Dictionary<string, object> posterData = posterSnapshot.ToDictionary();
                    arWorldMapController.Log("Loading poster " + posterData["id"] + " from chunk " + id);
                    // create poster
                    GameObject spotlight = Instantiate(arWorldMapController.spotlightPrefab, transform);

                    spotlight.GetComponent<Vertex>().topRadius = Convert.ToSingle(posterSnapshot.GetValue<double>("topR"));
                    spotlight.GetComponent<Vertex>().bottomRadius = Convert.ToSingle(posterSnapshot.GetValue<double>("botR"));

                    // NOT WORKING :(


                    float x = Convert.ToSingle(posterSnapshot.GetValue<double>("x"));
                    float y = Convert.ToSingle(posterSnapshot.GetValue<double>("y"));
                    float z = Convert.ToSingle(posterSnapshot.GetValue<double>("z"));

                    spotlight.transform.localPosition = new Vector3(x, y, z);

                    float rx = Convert.ToSingle(posterSnapshot.GetValue<double>("rx"));
                    float ry = Convert.ToSingle(posterSnapshot.GetValue<double>("ry"));
                    float rz = Convert.ToSingle(posterSnapshot.GetValue<double>("rz"));

                    spotlight.transform.localRotation = Quaternion.Euler(rx, ry, rz);

                    float sx = Convert.ToSingle(posterSnapshot.GetValue<double>("sx"));
                    float sy = Convert.ToSingle(posterSnapshot.GetValue<double>("sy"));
                    float sz = Convert.ToSingle(posterSnapshot.GetValue<double>("sz"));

                    spotlight.transform.localScale = new Vector3(sx, sy, sz);

                    arWorldMapController.Log("Loading spotlight");

                    spotlight.name = posterSnapshot.Id;
                    spotlight.tag = "element";
                    elements.Add(posterSnapshot.Id, spotlight);
                }

                // get posters from chunk
                CollectionReference filtersRef = db.Collection("maps").Document(arWorldMapController.worldMapId).Collection("chunks").Document(id).Collection("filters");
                QuerySnapshot filtersSnapshot = await filtersRef.GetSnapshotAsync();
                // go through posters and add them to the chunk
                foreach (DocumentSnapshot posterSnapshot in filtersSnapshot.Documents)
                {
                    Dictionary<string, object> posterData = posterSnapshot.ToDictionary();
                    arWorldMapController.Log("Loading poster " + posterData["id"] + " from chunk " + id);
                    // create poster
                    GameObject filterObj = Instantiate(arWorldMapController.filterPrefab, transform);

                    // spotlight.GetComponent<Vertex>().topRadius = Convert.ToSingle(posterSnapshot.GetValue<double>("topR"));
                    // spotlight.GetComponent<Vertex>().bottomRadius = Convert.ToSingle(posterSnapshot.GetValue<double>("botR"));

                    filter fil = filterObj.GetComponent<filter>();
                    fil.color = new Color(Convert.ToSingle(posterSnapshot.GetValue<double>("r")), Convert.ToSingle(posterSnapshot.GetValue<double>("g")), Convert.ToSingle(posterSnapshot.GetValue<double>("b")), Convert.ToSingle(posterSnapshot.GetValue<double>("a")));
                    fil.saturation = Convert.ToSingle(posterSnapshot.GetValue<double>("saturation"));
                    fil.threshold = Convert.ToSingle(posterSnapshot.GetValue<double>("threshold"));
                    fil.isColor = posterSnapshot.GetValue<bool>("isColor");

                    fil.innerFilter = arWorldMapController.innerFilter;
                    fil.camera = arWorldMapController.ARCamera;


                    float x = Convert.ToSingle(posterSnapshot.GetValue<double>("x"));
                    float y = Convert.ToSingle(posterSnapshot.GetValue<double>("y"));
                    float z = Convert.ToSingle(posterSnapshot.GetValue<double>("z"));

                    filterObj.transform.localPosition = new Vector3(x, y, z);

                    float rx = Convert.ToSingle(posterSnapshot.GetValue<double>("rx"));
                    float ry = Convert.ToSingle(posterSnapshot.GetValue<double>("ry"));
                    float rz = Convert.ToSingle(posterSnapshot.GetValue<double>("rz"));

                    filterObj.transform.localRotation = Quaternion.Euler(rx, ry, rz);

                    float sx = Convert.ToSingle(posterSnapshot.GetValue<double>("sx"));
                    float sy = Convert.ToSingle(posterSnapshot.GetValue<double>("sy"));
                    float sz = Convert.ToSingle(posterSnapshot.GetValue<double>("sz"));

                    filterObj.transform.localScale = new Vector3(sx, sy, sz);

                    arWorldMapController.Log("Loading filter");

                    filterObj.name = posterSnapshot.Id;
                    filterObj.tag = "element";

                    elements.Add(posterSnapshot.Id, filterObj);
                }

            }
        }

        async public void DownloadFile(string url, string filePath, float x, float y, float z, float rx, float ry, float rz, float sx, float sy, float sz, string id)
        {

            if (File.Exists(filePath))
            {
                arWorldMapController.Log("Found the same file locally, Loading!!!");

                LoadModel(filePath, x, y, z, rx, ry, rz, sx, sy, sz, id);

                return;
            }

            StartCoroutine(GetFileRequest(url, filePath, (UnityWebRequest req) =>
            {
                if (req.isNetworkError || req.isHttpError)
                {
                    //Logging any errors that may happen
                    arWorldMapController.Log($"{req.error} : {req.downloadHandler.text}");
                }

                else
                {
                    //Save the model fetched from firebase into spaceShip 
                    LoadModel(filePath, x, y, z, rx, ry, rz, sx, sy, sz, id);

                }
            }

            ));
        }

        private string preFilePath = "";

        void LoadModel(string path, float x, float y, float z, float rx, float ry, float rz, float sx, float sy, float sz, string id)
        {
            GameObject obj = Importer.LoadFromFile(path);
            obj.transform.parent = transform;

            obj.transform.localPosition = new Vector3(x, y, z);
            obj.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            obj.transform.localScale = new Vector3(sx, sy, sz);

            obj.name = id;
            obj.tag = "element";

            elements.Add(id, obj);
        }

        IEnumerator GetFileRequest(string url, string path, Action<UnityWebRequest> callback)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerFile(path);

                yield return req.SendWebRequest();

                callback(req);
            }
        }

    }
}