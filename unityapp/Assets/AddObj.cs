using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class AddObj : PressInputBase
    {
        [SerializeField]
        [Tooltip("Instantiates this prefab on a plane at the touch location.")]
        GameObject m_PlacedPrefab;

        /// <summary>
        /// The prefab to instantiate on touch.
        /// </summary>
        public GameObject placedPrefab
        {
            get { return m_PlacedPrefab; }
            set { m_PlacedPrefab = value; }
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

        protected override void Awake()
        {
            base.Awake();
            m_RaycastManager = GetComponent<ARRaycastManager>();
        }

        // protected override void OnPress

        protected override void OnPress(Vector3 position)
        {
            if (!isAdding)
            {
                Add();
                return;
            }



        }

        void Add()
        {
            isAdding = true;

            // raycast directly in front of camera to place object 0.5 units above plane hit relative to plane normal. If there is no plane hit, place object 0.5 units above camera
            if (m_RaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), s_Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = s_Hits[0].pose;

                // the rotation of the object is relative to the world, not the plane normal

                spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position + hitPose.rotation * Vector3.up * 0.1f, transform.rotation * Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + 180, 0));

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }
            else
            {
                spawnedObject = Instantiate(m_PlacedPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, transform.rotation * Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y + 180, 0));

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }
        }

        private void Update()
        {

            Debug.Log("touch count: " + Input.touchCount);
            Debug.Log("touch position: " + Input.GetMouseButton(0));
            if (Input.touchCount < 1 && !Input.GetMouseButton(0))
            {
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




            // if touch is on screen
            if (position.x > 0 && position.x < Screen.width && position.y > 0 && position.y < Screen.height)
            {
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
                    Debug.Log("plane normal: " + planeNormal);

                    // // get if object is behind plane
                    // bool isBehindPlane = Vector3.Dot(spawnedObject.transform.position - hitPose.position, planeNormal) < 0;

                    // get distance between object and plane only in the direction of the plane normal
                    float distance = Vector3.Dot(spawnedObject.transform.position - hitPose.position, planeNormal);

                    // move object to the plane
                    spawnedObject.transform.position = hitPose.position + hitPose.rotation * Vector3.up * Math.Max(distance, 0.1f);
                } else {
                    // if no plane is hit, move object to 0.5 units in front of camera at position of touch
                    Vector3 touchPosition = Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, 1.5f));
                    spawnedObject.transform.position = touchPosition;

                }
            }





        }
    }
}