// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.AR.HitTest;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Niantic.ARDK.Utilities.Input.Legacy;


namespace Niantic.ARDKExamples.RemoteAuthoring
{
  public class LocationManifestManager : MonoBehaviour
  {
    public event StatusLogChanged StatusLogChangeEvent;

    [SerializeField] private RuntimeVPSLocationManifest[] _manifests;

    [SerializeField] private GameObject[] _anchorPrefabArray; 

    [SerializeField] private GameObject[] _inventoryPNGColorArray; 

    [SerializeField] private GameObject[] _inventoryPNGBlackArray; 
    
    [SerializeField, HideInInspector] private bool syncPrefabSelection = true;
    [SerializeField, HideInInspector] private bool keepSynced = true;

    private static LocationManifestManager _instance;
    private Inventory inventory;
    [SerializeField] private UI_Inventory uiInventory;
    public static LocationManifestManager Instance => _instance;

    public WayspotManagerPOCO _wayspotManager;
    public String _authoredAnchorDefaultName = "Authored Anchor (Default)";

    private const string host = "http://192.168.103.213";
    private const string port = "5008";
    private const string get_endpoint = "getmanifest";

    private string _manifestJson;

    public string ManifestJson
    {
      get => _manifestJson;
      set => _manifestJson = value;
    }

    public bool SyncPrefabSelection
    {
      get => syncPrefabSelection;
      set => syncPrefabSelection = value;
    }

    public bool KeepSynced
    {
      get => keepSynced;
      set => keepSynced = value;
    }
    

    public RuntimeVPSLocationManifest[] Manifests
    {
      get => _manifests;
      set => _manifests = value;
    }

    [SerializeField] private AnchoredContent[] _anchoredContent;

    private readonly HashSet<AnchorStatusTracker> _anchorStatusTrackers = new HashSet<AnchorStatusTracker>();


    private void Start() {
      inventory = new Inventory();
      uiInventory.setInventory(inventory);
    }
    private void Awake()
    {
      _instance = this;
      _wayspotManager = new WayspotManagerPOCO();
      LoadManifestsFromMongo();
      AddWayspotManagerStatusListener(WayspotManagerOnStatusLogChangeEvent);
    }

    private void Update()
    {
      //Get the pose where you tap on the screen
      if (_wayspotManager.WayspotService.LocalizationState == LocalizationState.Localized)
      {
        Touch touch = Input.GetTouch(0);
        Ray raycast = Camera.main.ScreenPointToRay(touch.position);
        RaycastHit raycastHit;

        if (Physics.Raycast(raycast, out raycastHit))
        {
          int element_index = 0;
          foreach (var wayspotAnchorGameObject in _anchoredContent)
          {
            WayspotManagerOnStatusLogChangeEvent("RAYCAST NAME: " + raycastHit.collider.name);
            if (raycastHit.collider.name.ToLower().Equals(wayspotAnchorGameObject.ReadableName.ToLower().Split(new string[] { "handler" }, StringSplitOptions.None)[0])) 
            {
              Debug.Log("Tap on: " + wayspotAnchorGameObject.ReadableName);
              
              WayspotManagerOnStatusLogChangeEvent("Great! You found " + wayspotAnchorGameObject.Content.tag);
              if (touch.phase == TouchPhase.Ended){ 
                AddItemToInventory(element_index);
                raycastHit.collider.GetComponent<Animator>().SetBool("isRunning", true);
              }
              break;
            }
            element_index++;
          }
        }  
      }
    }

    private void AddItemToInventory(int index)
    {
      _inventoryPNGColorArray[index].SetActive(true);
      _inventoryPNGBlackArray[index].SetActive(false);
    }

    private void OnDestroy()
    {
      _wayspotManager.ShutDown();
    }

    public void PopulateAnchoredContentCustom()
    {
      List<AnchoredContent> anchoredContents = new List<AnchoredContent>();

      int locID = 0;
      for (int i = 0; i < _manifests.Length; i++)
      {
        var vpsLocationManifest = _manifests[i];
        var objScale = Vector3.one;
        foreach (var wayspotAnchorData in vpsLocationManifest.AuthoredAnchors)
        {
          if (wayspotAnchorData.Name.Equals(_authoredAnchorDefaultName))
          {
            continue;
          }
          GameObject obj = null;
          if (SyncPrefabSelection)
          {
            obj = _anchorPrefabArray[0];
            for (int j = 0; j < _anchorPrefabArray.Length; j++)
            {
              if (_anchorPrefabArray[j].name.Equals(wayspotAnchorData.Name.Split('_')[0]))
              {
                obj = _anchorPrefabArray[j];
                break;
              }
            }
          }

          var newContent =
            new AnchoredContent
            (
              locID,
              wayspotAnchorData.Name + "_" + vpsLocationManifest.LocationName, objScale,
              obj
            );

          anchoredContents.Add(newContent);
        }

        locID++;
      }

      _anchoredContent = anchoredContents.ToArray();
    }

    public void LoadManifestsFromMongo()
    {
      StartCoroutine(MongoGetRequest());
    }

    IEnumerator MongoGetRequest()
    {
      string url_get = host + ":" + port + "/" + get_endpoint;
      UnityWebRequest get_request = UnityWebRequest.Get(url_get);
      yield return get_request.SendWebRequest();
      if (get_request.result != UnityWebRequest.Result.Success) {
        Debug.Log(get_request.error);
      }
      else 
      {
        // se la risposta non è vuota, seleziona i payload
        var jsonText = get_request.downloadHandler.text;
        _manifestJson = jsonText;
        var manifest = JsonUtility.FromJson<RuntimeVPSLocationManifest>(_manifestJson);
        _manifests = new RuntimeVPSLocationManifest[] {manifest};
        PopulateAnchoredContentCustom();
      }
    }

    private void WayspotManagerOnStatusLogChangeEvent(string statusmessage)
    {
      StatusLogChangeEvent?.Invoke(statusmessage);
    }

    /// Loads all of the saved wayspot anchors
    public void LoadWayspotAnchors(int locationID)
    {
      ClearAnchorGameObjects();
      _wayspotManager.StartOrRestartWayspotAnchorService();

      //get content from anchor content list
      AnchoredContent[] filteredContent = GetFilteredAnchorContentFromLocation(locationID);
      if (filteredContent.Length > 0)
      {
        foreach (var anchoredContent in filteredContent)
        {
          var payload = GetPayloadFromAnchorData(locationID, anchoredContent);
          if (!_wayspotManager.RestoreAnchorsWithPayload(out var anchors, payload))
          {
            continue;
          }

          var prefab = GetGameObjectFromAnchorData(anchoredContent);
          if (prefab != null)
            CreateWayspotAnchorGameObject(anchors[0], prefab, anchoredContent.ContentScale);
        }

        OnAnchorStatusCodeUpdated(null);
      }
      else
      {
        StatusLogChangeEvent?.Invoke("No anchors to load.");
      }
    }

    public AnchoredContent[] GetFilteredAnchorContentFromLocation(int locationID)
    {
      List<AnchoredContent> anchorContentList = new List<AnchoredContent>();
      foreach (var anchorContent in _anchoredContent)
      {
        if (anchorContent.ManifestID == locationID)
          anchorContentList.Add(anchorContent);
      }

      return anchorContentList.ToArray();
    }

    public WayspotAnchorPayload GetPayloadFromAnchorData(int locationID, AnchoredContent anchoredContent)
    {
      if (!string.IsNullOrEmpty(anchoredContent.ReadableName))
      {
        var authoredAnchorData = FindAnchorDataFromName(_manifests[locationID], anchoredContent.ReadableName.Replace("_"+
          _manifests[locationID].LocationName, ""));
        if (authoredAnchorData != null)
        {
          return WayspotAnchorPayload.Deserialize(authoredAnchorData.Payload);
        }
      }

      Debug.LogError("Error retrieving anchored Content payload");
      return null;
    }

    public RuntimeAuthoredWayspotAnchorData FindAnchorDataFromName(RuntimeVPSLocationManifest manifest, string name)
    {
      foreach (var authoredWayspotAnchorData in manifest.AuthoredAnchors)
      {
        if (authoredWayspotAnchorData.Name == name)
        {
          return authoredWayspotAnchorData;
        }
      }
      Debug.LogError("Error: Name" +name+" does not exist in this location Manifest anymore!");
      return null;
    }
    
    public GameObject GetGameObjectFromAnchorData(AnchoredContent anchoredContent)
    {
      if (!string.IsNullOrEmpty(anchoredContent.ReadableName) && anchoredContent.Content != null)
        return anchoredContent.Content;

      Debug.LogError("Error retrieving anchored Content gameobject");
      return null;
    }

    private void RemoveAllContent()
    {
      _manifests = Array.Empty<RuntimeVPSLocationManifest>();
      _anchoredContent = Array.Empty<AnchoredContent>();
    }

    /// Clears all of the active wayspot anchors
    public void ClearAnchorGameObjects()
    {
      if (_anchorStatusTrackers.Count == 0)
      {
        StatusLogChangeEvent?.Invoke("No anchors to clear.");
        return;
      }

      foreach (var anchor in _anchorStatusTrackers)
        Destroy(anchor.gameObject);

      var wayspotAnchors = _anchorStatusTrackers.Select(go => go.WayspotAnchor).ToArray();
      _wayspotManager.DestroyAnchors(wayspotAnchors);

      _anchorStatusTrackers.Clear();
      StatusLogChangeEvent?.Invoke("Cleared Wayspot Anchors.");
    }

    // Invoked whenever any anchor's status updates
    private void OnAnchorStatusCodeUpdated(WayspotAnchorStatusUpdate args)
    {
      // Update the status message for the tracked anchors.
      if (_anchorStatusTrackers.Count > 0)
      {
        var numPending = 0;
        var numSuccess = 0;
        var numFailed = 0;
        var numInvalid = 0;
        foreach (var wayspotAnchorTracker in _anchorStatusTrackers)
        {
          switch (wayspotAnchorTracker.WayspotAnchor.Status)
          {
            case WayspotAnchorStatusCode.Pending:
              numPending++;
              break;

            case WayspotAnchorStatusCode.Limited:
            case WayspotAnchorStatusCode.Success:
              numSuccess++;
              break;

            case WayspotAnchorStatusCode.Failed:
              numFailed++;
              break;

            case WayspotAnchorStatusCode.Invalid:
              numInvalid++;
              break;
          }
        }
        
        string statusText = "Anchor Status:\n"
            + $"Pending: {numPending}, Failed: {numFailed}, Invalid: {numInvalid}, Resolved: {numSuccess}.";
        StatusLogChangeEvent?.Invoke(statusText);
      }
    }

    private GameObject CreateWayspotAnchorGameObject
    (
      IWayspotAnchor anchor,
      GameObject anchorPrefab,
      Vector3 scale
    )
    {
      var go = Instantiate(anchorPrefab);
      go.transform.localScale = scale;
      
      var tracker = go.GetComponent<AnchorStatusTracker>();
      if (tracker == null)
      {
        Debug.Log("Anchor prefab was missing AnchorStatusTracker, so one will be added.");
        tracker = go.AddComponent<AnchorStatusTracker>();
      }

      tracker.AttachAnchor(anchor);
      _anchorStatusTrackers.Add(tracker);

      anchor.StatusCodeUpdated += OnAnchorStatusCodeUpdated;

      return go;
    }

    public string[] GetLocationNames()
    {
      string[] locationNames = new string[_manifests.Length];
      for (int i = 0; i < _manifests.Length; i++)
      {
        locationNames[i] = _manifests[i].LocationName;
      }

      return locationNames;
    }

    public void AddWayspotManagerStatusListener(StatusLogChanged listener)
    {
      _wayspotManager.StatusLogChangeEvent += listener;
    }
    public void AddLocalizationStatusListener(LocalizationStatusChanged listener)
    {
      _wayspotManager.LocalizationStatusChangeEvent += listener;
    }


    public RuntimeVPSLocationManifest[] SyncManifestsCustom(RuntimeVPSLocationManifest[] manifests)
    {
      var updatedManifests = manifests.ToList();
      var manifest = JsonUtility.FromJson<RuntimeVPSLocationManifest>(_manifestJson);
      updatedManifests[3] = manifest;
      return updatedManifests.ToArray();
    }

#if UNITY_EDITOR
    public RuntimeVPSLocationManifest[] SyncManifests(RuntimeVPSLocationManifest[] manifests)
    {
      var updatedManifests = manifests.ToList();

      //find a copy of the real Manifest locally
      //if it exists, replace it!
      var guids = UnityEditor.AssetDatabase.FindAssets("t:VPSLocationManifest");

      for (int i = 0; i < guids.Length; i++)
      {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
        var manifest = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) as VPSLocationManifest;

        if (manifest)
        {
          var index = updatedManifests.FindIndex(p => p.LocationName == manifest.LocationName);
          if (index >= 0)
          {
            updatedManifests[index] = new RuntimeVPSLocationManifest(manifest);
          }
        }
      }

      return updatedManifests.ToArray();
    }
    public void PopulateAnchoredContent()
    {
      List<AnchoredContent> anchoredContents = new List<AnchoredContent>();
      _manifests = SyncManifests(_manifests);
      int locID = 0;
      for (int i = 0; i < _manifests.Length; i++)
      {
        var vpsLocationManifest = _manifests[i];
        var objScale = Vector3.one;
        foreach (var wayspotAnchorData in vpsLocationManifest.AuthoredAnchors)
        {
          GameObject obj = null;
          if (SyncPrefabSelection)
          {
            //you can only read from associated prefabs in editor
            //if trying to do this in a build, you'll likely want to create your own lookup dictionary
            //using anchor identifier as a reference
            obj = wayspotAnchorData.GetAssociatedEditorPrefab(vpsLocationManifest.LocationName, out objScale);
          }

          var newContent =
            new AnchoredContent
            (
              locID,
              wayspotAnchorData.Name + "_"+vpsLocationManifest.LocationName, objScale,
              obj
            );

          anchoredContents.Add(newContent);
        }

        locID++;
      }

      _anchoredContent = anchoredContents.ToArray();
    }
#endif
  }
}