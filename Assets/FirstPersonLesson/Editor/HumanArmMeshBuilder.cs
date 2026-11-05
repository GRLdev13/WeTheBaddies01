using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Edition seulement : reutilise les avant-bras ET les mains du personnage humain destination.
public static class HumanArmMeshBuilder
{
    private const string Root = "Assets/FirstPersonLesson";

    [MenuItem("School/Replace first person arms with human arms")]
    public static void ReplaceInScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Quitter Play avant modification.");
        var rig = UnityEngine.Object.FindAnyObjectByType<FirstPersonArms>();
        if (rig == null) throw new InvalidOperationException("Bras premiere personne introuvables.");
        UpdateRig(rig.transform);
        PrefabUtility.SaveAsPrefabAsset(rig.gameObject, Root + "/Prefabs/FirstPersonArms.prefab");
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    public static void UpdateRig(Transform rig)
    {
        var human = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Character/Model/Prefab/HumanMale_Character_FREE.prefab");
        var renderers = human.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var arms = renderers.Single(r => r.sharedMesh != null && r.sharedMesh.name == "Body_Arms");
        var hands = renderers.Single(r => r.sharedMesh != null && r.sharedMesh.name == "Body_Hands");
        foreach (string side in new[] { "Left", "Right" })
        {
            string suffix = side == "Left" ? "_L" : "_R";
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            AppendPart(arms, suffix, false, vertices, uv, triangles);
            AppendPart(hands, suffix, true, vertices, uv, triangles);
            var generated = new Mesh { name = side + "HumanForearmAndHand" };
            generated.SetVertices(vertices);
            generated.SetUVs(0, uv);
            generated.SetTriangles(triangles, 0);
            generated.RecalculateNormals();
            generated.RecalculateBounds();
            string path = Root + "/" + side + "Forearm.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = generated; AssetDatabase.CreateAsset(mesh, path); }
            else
            {
                // Conserver le GUID preserve les references deja presentes dans les prefabs.
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }
            Transform arm = rig.Find(side + "Arm");
            if (arm == null)
            {
                arm = new GameObject(side + "Arm", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                arm.SetParent(rig, false);
            }
            arm.localPosition = new Vector3(side == "Left" ? -0.22f : 0.22f, -0.24f, 0.18f);
            arm.localRotation = Quaternion.Euler(0, side == "Left" ? 12f : -12f, 0);
            arm.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = arm.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = arms.sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private static void AppendPart(SkinnedMeshRenderer renderer, string suffix, bool isHand,
        List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
    {
        Mesh source = renderer.sharedMesh;
        string[] names = renderer.bones.Select(b => b.name).ToArray();
        int elbowIndex = Array.IndexOf(names, "Elbow" + suffix);
        int wristIndex = Array.IndexOf(names, "Wrist" + suffix);
        Vector3 elbow = source.bindposes[elbowIndex].inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 wrist = source.bindposes[wristIndex].inverse.MultiplyPoint3x4(Vector3.zero);
        Quaternion rotation = Quaternion.FromToRotation(wrist - elbow, Vector3.forward);
        float scale = 0.23f / Vector3.Distance(elbow, wrist);
        var weights = source.boneWeights;
        bool IncludedBone(int i) => isHand ? names[i].EndsWith(suffix) &&
            (names[i].Contains("Finger") || names[i].StartsWith("Wrist")) : i == elbowIndex || i == wristIndex;
        bool IncludedVertex(int i)
        {
            var w = weights[i];
            float sum = (IncludedBone(w.boneIndex0) ? w.weight0 : 0) +
                (IncludedBone(w.boneIndex1) ? w.weight1 : 0) +
                (IncludedBone(w.boneIndex2) ? w.weight2 : 0) +
                (IncludedBone(w.boneIndex3) ? w.weight3 : 0);
            return sum > 0.5f;
        }
        Vector3[] original = source.vertices;
        Vector2[] originalUV = source.uv;
        var map = new Dictionary<int, int>();
        int Remap(int i)
        {
            if (map.TryGetValue(i, out int existing)) return existing;
            int index = vertices.Count;
            map.Add(i, index);
            // La pose de liaison fournit le meme repere pour les maillages bras et mains.
            vertices.Add(rotation * (original[i] - elbow) * scale);
            uv.Add(originalUV[i]);
            return index;
        }
        int[] indices = source.triangles;
        int count = triangles.Count;
        for (int i = 0; i < indices.Length; i += 3)
            if (IncludedVertex(indices[i]) && IncludedVertex(indices[i + 1]) && IncludedVertex(indices[i + 2]))
            {
                triangles.Add(Remap(indices[i]));
                triangles.Add(Remap(indices[i + 1]));
                triangles.Add(Remap(indices[i + 2]));
            }
        if (triangles.Count == count) throw new InvalidOperationException("Aucun triangle humain extrait : " + renderer.name + suffix);
    }
}
