using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Outil d'edition uniquement : les prefabs generes sont des objets Unity ordinaires.
public static class FirstPersonLessonSetup
{
    private const string Root = "Assets/FirstPersonLesson";

    [MenuItem("School/Install first person lesson in current scene")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Quitter Play avant installation.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null) throw new InvalidOperationException("Ouvrir DebugScene (avec PlayerController).");
        var camera = player.playercamera != null ? player.playercamera : player.GetComponentInChildren<Camera>();
        if (camera == null) throw new InvalidOperationException("Camera du joueur introuvable.");
        if (player.GetComponentInChildren<FirstPersonArms>() != null)
            throw new InvalidOperationException("Le cours est deja installe dans cette scene.");

        var palette = AssetDatabase.LoadAssetAtPath<Material>(Root + "/SourcePalette.mat");
        bool newMaterial = palette == null;
        if (newMaterial) palette = new Material(Shader.Find("Standard"));
        palette.name = "SourcePalette";
        palette.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Source/TEX_GradientPalette.png");
        palette.SetFloat("_Glossiness", 0.15f);
        if (newMaterial) AssetDatabase.CreateAsset(palette, Root + "/SourcePalette.mat");

        var rig = new GameObject("FirstPersonArms");
        rig.transform.SetParent(camera.transform, false);
        rig.AddComponent<FirstPersonArms>();
        HumanArmMeshBuilder.UpdateRig(rig.transform);
        var hand = new GameObject("HandAnchor").transform;
        hand.SetParent(rig.transform, false);
        hand.localPosition = new Vector3(0.17f, -0.24f, 0.41f);
        player.GetComponent<PlayerInteractBehavior>().SetHandAnchor(hand);
        camera.nearClipPlane = 0.03f;

        var pickaxe = MakeTool("Pickaxe", "Pioche", "SM_Pickaxe_Tier1", palette,
            new Vector3(0, -0.12f, 0), new Vector3(0, 0, 15));
        var bucket = MakeTool("Bucket", "Seau", "SM_Bucket_Tier1", palette,
            new Vector3(0, -0.30f, 0), Vector3.zero);
        // Les objets sont places devant le point de depart, sur le sol detecte par un rayon.
        Place(pickaxe, player.transform.position + player.transform.forward * 2f - player.transform.right * 0.6f);
        Place(bucket, player.transform.position + player.transform.forward * 2f + player.transform.right * 0.6f);
        PrefabUtility.SaveAsPrefabAsset(rig, Root + "/Prefabs/FirstPersonArms.prefab");
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject MakeTool(string name, string label, string meshName, Material material,
        Vector3 heldPosition, Vector3 heldAngles)
    {
        var go = new GameObject(name);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Source/" + meshName + ".asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        var collider = go.AddComponent<BoxCollider>();
        collider.center = mesh.bounds.center;
        collider.size = mesh.bounds.size;
        var body = go.AddComponent<Rigidbody>();
        body.mass = name == "Bucket" ? 1.5f : 2f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        go.AddComponent<PickupItem>().Configure(label, heldPosition, heldAngles);
        PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab");
        return go;
    }

    private static void Place(GameObject go, Vector3 point)
    {
        float floor = point.y;
        if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var hit, 10f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) floor = hit.point.y;
        var bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
        go.transform.position = new Vector3(point.x, floor - bounds.min.y + 0.03f, point.z);
    }

}

