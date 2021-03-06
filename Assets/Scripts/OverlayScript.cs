﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Globalization;

namespace grubFX
{
    public class OverlayScript : MonoBehaviour
    {
        private OverlayData overlayData;
        private ArrayList locationObjects, pathsObjects;
        private Slider episodeSlider;
        private Text episodeLabel;
        private TextMesh tagLabel;
        private Camera arCamera;
        private Material myMaterial;
        private GameObject locationTag, hitObject, rotationPoint, locationTagBox;
        private Ray ray;
        private RaycastHit hit;

        // Use this for initialization
        void Start()
        {
            if (locationTag == null)
            {
                locationTag = GameObject.Find("LocationTag");
            }
            locationTag.SetActive(false);

            myMaterial = new Material(Shader.Find("Sprites/Default"));

            episodeSlider = GameObject.Find("EpisodeSlider").GetComponent<Slider>();
            episodeSlider?.onValueChanged.AddListener(delegate { ReactOnSliderValueChange(); });

            episodeLabel = GameObject.Find("EpisodeLabel").GetComponent<Text>();
        }

        public void ReactOnSliderValueChange()
        {
            Debug.Log("slider value changed to " + episodeSlider?.value);
            SetEpisodeLabelOfIndex((int)episodeSlider.value);
            DrawPaths();
        }

        void Update()
        {
            // find object hit by Raycast after Touch
            if (arCamera == null)
            {
                arCamera = GameObject.Find("ARCamera").GetComponent<Camera>();
            }
            if (locationTag == null)
            {
                locationTag = GameObject.Find("LocationTag");
            }
            if (rotationPoint == null)
            {
                rotationPoint = GameObject.Find("RotationPoint");
            }

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        ray = arCamera.ScreenPointToRay(touch.position);
                    }
                    break;

                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXPlayer:
                default:
                    ray = arCamera.ScreenPointToRay(Input.mousePosition);
                    break;
            }

            if (Physics.Raycast(ray, out hit))
            {
                hitObject = hit.collider.gameObject;
                //Debug.Log("hit " + hitObject.name);
                if (!hitObject.name.Equals("Board") && !hitObject.name.Equals("Triangle"))
                {
                    // show locationTag when object was touched
                    locationTag.SetActive(true);
                    if (tagLabel == null)
                    {
                        tagLabel = GameObject.Find("TagLabel").GetComponent<TextMesh>();
                    }
                    tagLabel.text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(hitObject.name.Replace("_", " ").Replace("%27", "'"));

                    if (locationTagBox == null)
                    {
                        locationTagBox = GameObject.Find("LocationTagBox");
                    }
                    // hover over touched object
                    locationTagBox.transform.position = hitObject.transform.position;
                }
            }

            if (locationTag && locationTag.activeSelf)
            {
                //locationTag.transform.SetPositionAndRotation(locationTag.transform.position, rot * arCamera.transform.rotation);
                //locationTag.transform.RotateAround(rotationPoint.transform.position, Vector3.forward, arCamera.transform.rotation.y);

                //Debug.Log("angle: " + rot.eulerAngles.y + "\t/ sin: " + Mathf.Sin(rot.eulerAngles.y / ((float)Math.PI * 2f)) + "\t/ cos:" + Mathf.Cos(rot.eulerAngles.y / ((float)Math.PI * 2f)));
                Vector3 rot = arCamera.transform.rotation.eulerAngles;
                locationTag.transform.RotateAround(rotationPoint.transform.position, Vector3.up, rot.x - locationTag.transform.rotation.eulerAngles.x);
                locationTag.transform.RotateAround(rotationPoint.transform.position, Vector3.up, rot.y - locationTag.transform.rotation.eulerAngles.y);
                locationTag.transform.RotateAround(rotationPoint.transform.position, Vector3.up, rot.z - locationTag.transform.rotation.eulerAngles.z);
            }
        }

        /*
        public Vector3 PolarToCartesian(float lat, float lng)
        {
            var origin = new Vector3(0, 0, 1);
            var rotation = Quaternion.Euler(lat, lng, 0);
            Vector3 point = rotation * origin;
            return point;
        }
        */

        private Vector3 LatLngToXY(Coords input)
        {
            float radius = 250;
            Vector3 v;
            /*
            v.x = radius * Mathf.Cos(ToRadians(input.Lat)) * Mathf.Sin(ToRadians(input.Long));
            v.y = radius * Mathf.Sin(ToRadians(input.Lat));
            v.z = radius * Mathf.Cos(ToRadians(input.Lat)) * Mathf.Cos(ToRadians(input.Long));
            */
            v.x = (float)(input.Long * 1.39);
            v.y = 0;
            v.z = (float)(input.Lat * (1.34 + 0.000685 * input.Lat + 0.000141 * input.Lat * input.Lat));
            return v;
        }

        private float ToRadians(double degrees)
        {
            return (float)degrees * Mathf.Deg2Rad;
        }

        public void DrawOverlayData(OverlayData data)
        {
            overlayData = data;

            if (episodeSlider != null)
            {
                episodeSlider.maxValue = overlayData.EpisodeList.Count - 1;
            }
            if (episodeLabel != null)
            {
                SetEpisodeLabelOfIndex(0);
            }

            Debug.Log("starting drawing of overlay");


            // locations
            DrawLocations();

            // paths
            DrawPaths();
        }

        private void DrawPaths()
        {
            pathsObjects = DeepCleanListAndReturn(pathsObjects);
            foreach (PathsPerPerson pathsPerPerson in overlayData.AllPathsList)
            {
                foreach (Path path in pathsPerPerson.PathList)
                {
                    if (episodeSlider != null && path.Episodes.Contains((int)episodeSlider.value))
                    {
                        DrawSinglePath(path, pathsPerPerson.Name);
                    }
                }
            }
        }

        private void DrawLocations()
        {
            locationObjects = DeepCleanListAndReturn(locationObjects);
            foreach (Location l in overlayData.LocationList)
            {
                if (l != null && l.Coords != null && l.Coords.Lat != 0 && l.Coords.Long != 0)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = l.Key.Replace("-", " ");
                    cube.GetComponent<Renderer>().material.color = Color.gray;
                    // Vector3 converted = PolarToCartesian((float)l.Coords.Lat, (float)l.Coords.Long);
                    // GameObject target = GameObject.Find("ImageTarget");
                    // float scale = target.transform.localScale.x;
                    //cube.transform.position = new Vector3((float)l.Coords.Long, 1, (float)l.Coords.Lat);
                    cube.transform.position = LatLngToXY(l.Coords);
                    locationObjects.Add(cube);
                }
            }
        }

        private void DrawSinglePath(Path path, String name)
        {
            Color newColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

            // SingleCoords
            if (path.PointList.Count == 0 && path.SingleCoords.Lat != 0 && path.SingleCoords.Long != 0)
            {
                sphere.GetComponent<Renderer>().material.color = newColor;
                sphere.transform.localScale = new Vector3(2, 2, 2);
                sphere.transform.position = LatLngToXY(path.SingleCoords);
                pathsObjects.Add(sphere);
            }

            // Path
            else if (path.PointList.Count > 0)
            {
                sphere.AddComponent<LineRenderer>();
                LineRenderer lineRenderer = sphere.GetComponent<LineRenderer>();
                lineRenderer.positionCount = path.PointList.Count;
                pathsObjects.Add(sphere); // in order to later get the LineRenderer from it to delete it

                // -- draw all
                for (int i = 0; i < path.PointList.Count; i++)
                {
                    if (sphere == null)
                    {
                        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    }
                    sphere.name = name;
                    sphere.GetComponent<Renderer>().material.color = newColor;
                    sphere.transform.localScale = new Vector3(2, 2, 2);
                    Coords c = path.PointList[i];
                    if (c.Lat != 0 && c.Long != 0)
                    {
                        Vector3 p = LatLngToXY(c);
                        sphere.transform.position = p;
                        lineRenderer.SetPosition(i, p);
                        lineRenderer.startWidth = 2;
                        lineRenderer.endWidth = 2;
                        lineRenderer.material = myMaterial;
                        lineRenderer.material.color = newColor;
                        lineRenderer.startColor = newColor;
                        lineRenderer.endColor = newColor;
                        pathsObjects.Add(sphere);
                        sphere = null;
                    }
                }
            }
        }

        public void StopDrawingOverlay()
        {
            Debug.Log("stopping drawing of overlay");

            locationTag?.SetActive(false);

            DeepCleanListAndReturn(locationObjects);
            DeepCleanListAndReturn(pathsObjects);
        }

        private void SetEpisodeLabelOfIndex(int index)
        {
            episodeLabel.text = ((Episode)overlayData.EpisodeList[index]).Title;
        }

        private ArrayList DeepCleanListAndReturn(ArrayList list)
        {
            if (list == null)
            {
                list = new ArrayList();
            }
            else
            {
                if (list.Count > 0)
                {
                    foreach (GameObject o in list)
                    {
                        LineRenderer lr = o.GetComponent<LineRenderer>();
                        if (lr != null)
                        {
                            lr.positionCount = 0;
                            Destroy(lr);
                        }
                        Destroy(o);
                    }
                }
                list.Clear();
            }
            return list;
        }
    }
}