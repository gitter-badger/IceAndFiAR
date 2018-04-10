﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace grubFX
{
    public class OverlayScript : MonoBehaviour
    {
        private OverlayData overlayData;
        private ArrayList locationObjects, pathsObjects;
        private Slider episodeSlider;
        private Text episodeLabel;
        private Camera arCamera;
        private Material myMaterial;
        private GameObject locationTag, hitObject;
        private Ray ray;
        private RaycastHit hit;
        private Vector3 pos;
        private Quaternion rot;

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

            rot = Quaternion.Euler(new Vector3(-45, 0, 0));
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
            ray = arCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                hitObject = hit.collider.gameObject;
                Debug.Log("hit " + hitObject.name);
                if (!hitObject.name.Equals("Board") && !hitObject.name.Equals("Triangle"))
                {
                    // show locationTag when object was touched
                    if (locationTag == null)
                    {
                        locationTag = GameObject.Find("LocationTag");
                    }
                    locationTag.SetActive(true);
                    // hover over touched object
                    pos = new Vector3(hitObject.transform.position.x, hitObject.transform.position.y + 16, hitObject.transform.position.z + 16);
                    locationTag.transform.SetPositionAndRotation(pos, rot * arCamera.transform.rotation);
                }
            }
            if (locationTag)
            {
                locationTag.transform.SetPositionAndRotation(locationTag.transform.position, rot * arCamera.transform.rotation);
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
                    cube.name = l.Key;
                    cube.GetComponent<Renderer>().material.color = Color.gray;
                    // Vector3 converted = PolarToCartesian((float)l.Coords.Lat, (float)l.Coords.Long);
                    // GameObject target = GameObject.Find("ImageTarget");
                    // float scale = target.transform.localScale.x;
                    cube.transform.position = new Vector3((float)l.Coords.Long, 1, (float)l.Coords.Lat);
                    locationObjects.Add(cube);
                }
            }
        }

        private void DrawSinglePath(Path path, String name)
        {
            Color newColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            // SingleCoords
            if (path.SingleCoords.Lat != 0 && path.SingleCoords.Long != 0)
            {
                sphere.GetComponent<Renderer>().material.color = newColor;
                sphere.transform.localScale = new Vector3(2, 2, 2);
                sphere.transform.position = new Vector3((float)path.SingleCoords.Long, 1, (float)path.SingleCoords.Lat);
                pathsObjects.Add(sphere);
            }

            // Path
            if (path.PointList.Count > 0)
            {
                sphere.AddComponent<LineRenderer>();
                LineRenderer lineRenderer = sphere.GetComponent<LineRenderer>();
                lineRenderer.positionCount = path.PointList.Count;
                pathsObjects.Add(sphere); // in order to later get the LineRenderer from it to delete it

                // -- draw all
                for (int i = 0; i < path.PointList.Count; i++)
                {
                    sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = name;
                    sphere.GetComponent<Renderer>().material.color = newColor;
                    sphere.transform.localScale = new Vector3(2, 2, 2);
                    Coords c0 = path.PointList[i];
                    Vector3 p = new Vector3((float)c0.Long, 1, (float)c0.Lat);
                    sphere.transform.position = p;
                    lineRenderer.SetPosition(i, p);
                    lineRenderer.startWidth = 2;
                    lineRenderer.endWidth = 2;
                    lineRenderer.material = myMaterial;
                    lineRenderer.material.color = newColor;
                    lineRenderer.startColor = newColor;
                    lineRenderer.endColor = newColor;
                    pathsObjects.Add(sphere);
                }
            }
        }

        public void StopDrawingOverlay()
        {
            Debug.Log("stopping drawing of overlay");

            if (locationTag)
            {
                locationTag.SetActive(false);
            }

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