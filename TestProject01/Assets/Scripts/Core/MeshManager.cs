using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    public class MeshManager : MonoBehaviour
    {
        public GameObject sourceMesh;
        public string rootBoneName;
        public bool clearChildrenAfterModify;

        public List<string> whichMeshes;
        public List<GameObject> whatMeshes;

        [HideInInspector] [SerializeField] private GameObject avatarMeshGo;

        private readonly Dictionary<string, Transform> _bonesMap = new Dictionary<string, Transform>();

        public SkinnedMeshRenderer CurrentSmr { get; private set; }

        public UnityEvent onMeshReset;

        private void Awake()
        {
            if (avatarMeshGo)
            {
                CurrentSmr = avatarMeshGo.GetComponent<SkinnedMeshRenderer>();
            }
        }

        public void CreateNewMesh()
        {
            PrepareCreate();

            if (!InitBaseMesh()) return;
            if (!CollectAllBones()) return;

            AddWhatMeshes();
            RemoveWhichMeshes();
            CombineMesh(avatarMeshGo.transform);

            CompleteCreate();
        }

        private void CompleteCreate()
        {
            ClearScrapGameObject(avatarMeshGo.transform);

            if (avatarMeshGo)
            {
                avatarMeshGo.SetActive(true);
            }

            onMeshReset.Invoke();
        }

        private void PrepareCreate()
        {
            if (avatarMeshGo)
            {
                DestroyCompat(avatarMeshGo);
            }

            for (var i = 0; i < transform.childCount; i++)
            {
                DestroyCompat(transform.GetChild(i).gameObject);
            }
        }

        private bool InitBaseMesh()
        {
            if (!sourceMesh) return false;

            avatarMeshGo = Instantiate(sourceMesh, transform);
            avatarMeshGo.SetActive(false);
            return true;
        }

        private bool CollectAllBones()
        {
            _bonesMap.Clear();
            var rootBone = avatarMeshGo.transform.Find(rootBoneName);
            if (rootBone)
            {
                BonesTravel(rootBone, _bonesMap);
                return true;
            }

            return false;
        }

        private void RemoveWhichMeshes()
        {
            foreach (var whichMesh in whichMeshes)
            {
                if (string.IsNullOrEmpty(whichMesh)) continue;
                var which = avatarMeshGo.transform.Find(whichMesh);
                if (which)
                {
                    DestroyCompat(which.gameObject);
                }
            }
        }

        private void AddWhatMeshes()
        {
            foreach (var whatMesh in whatMeshes)
            {
                if (!whatMesh) continue;
                var what = Instantiate(whatMesh, avatarMeshGo.transform).transform;
                what.position = avatarMeshGo.transform.position;
                what.rotation = avatarMeshGo.transform.rotation;
                CombineBones(what);
            }
        }

        private void BonesTravel(Transform bone, Dictionary<string, Transform> bones)
        {
            if (!bone || bone.childCount == 0) return;
            for (var i = 0; i < bone.childCount; i++)
            {
                var childBone = bone.GetChild(i);
                bones[childBone.name] = childBone;
                BonesTravel(childBone, bones);
            }
        }

        private void CombineBones(Transform what)
        {
            var whatSmr = what.GetComponentInChildren<SkinnedMeshRenderer>();
            var realBones = new List<Transform>();
            foreach (var bone in whatSmr.bones)
            {
                if (!_bonesMap.ContainsKey(bone.name) && _bonesMap.ContainsKey(bone.parent.name))
                {
                    var parentBone = _bonesMap[bone.parent.name];
                    var newBone = Instantiate(bone, parentBone);
                    newBone.name = bone.name;
                    _bonesMap[newBone.name] = newBone;
                    BonesTravel(newBone, _bonesMap);
                }
            }

            realBones.AddRange(whatSmr.bones.Select(bone => _bonesMap[bone.name]));

            whatSmr.bones = realBones.ToArray();
        }

        private void CombineMesh(Transform bashMeshTransform)
        {
            var skinnedMeshRenderers = bashMeshTransform.GetComponentsInChildren<SkinnedMeshRenderer>();

            var combineInstances = new List<CombineInstance>();
            var materials = new List<Material>();
            var bones = new List<Transform>();

            foreach (var smr in skinnedMeshRenderers)
            {
                // materials.AddRange(smr.sharedMaterials.Select(Instantiate));
                materials.AddRange(smr.sharedMaterials);

                for (var sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
                {
                    bones.AddRange(smr.bones);
                    var ci = new CombineInstance
                    {
                        mesh = smr.sharedMesh,
                        subMeshIndex = sub
                    };
                    combineInstances.Add(ci);
                }
            }

            var newMesh = new Mesh();
            newMesh.CombineMeshes(combineInstances.ToArray(), false, false);

            if (newMesh.vertexCount <= 0)
            {
                Debug.LogError("vertex count is zero!!!! maybe you should CHECK 'Read/Write' in model inspector!!!!");
            }

            CombineBlendShapes(skinnedMeshRenderers, newMesh);

            CurrentSmr = bashMeshTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            CurrentSmr.sharedMesh = newMesh;
            CurrentSmr.bones = bones.ToArray();
            CurrentSmr.materials = materials.ToArray();
        }

        private void ClearScrapGameObject(Transform bashMeshTransform)
        {
            var childrenList = new List<Transform>();
            for (var i = 0; i < bashMeshTransform.childCount; i++)
            {
                var child = bashMeshTransform.GetChild(i);
                if (string.Equals(child.name, rootBoneName) || child.gameObject == avatarMeshGo)
                {
                    continue;
                }

                childrenList.Add(child);
            }

            var childCount = childrenList.Count;
            for (var i = 0; i < childCount; i++)
            {
                var childTf = childrenList[i];
                if (clearChildrenAfterModify)
                {
                    DestroyCompat(childTf.gameObject);
                }
                else
                {
                    childTf.gameObject.SetActive(false);
                }
            }
        }

        private void CombineBlendShapes(SkinnedMeshRenderer[] skinnedMeshRenderers, Mesh newMesh)
        {
            var vertexList = new List<Vector3>();
            var normalList = new List<Vector3>();
            var tangentList = new List<Vector3>();
            var vertexCounter = 0;

            foreach (var smr in skinnedMeshRenderers)
            {
                for (var i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                {
                    var shapeName = smr.sharedMesh.GetBlendShapeName(i);
                    var shapeFrameCount = smr.sharedMesh.GetBlendShapeFrameCount(i);

                    for (var j = 0; j < shapeFrameCount; j++)
                    {
                        var sharedMesh = smr.sharedMesh;
                        var vertices = new Vector3[sharedMesh.vertexCount];
                        var normals = new Vector3[sharedMesh.vertexCount];
                        var tangents = new Vector3[sharedMesh.vertexCount];

                        smr.sharedMesh.GetBlendShapeFrameVertices(i, j, vertices, normals, tangents);
                        smr.sharedMesh.GetBlendShapeFrameWeight(i, j);
                        var weight = smr.sharedMesh.GetBlendShapeFrameWeight(i, j);

                        FillByVertexCount(newMesh.vertexCount, vertexCounter, vertexList, normalList,
                            tangentList, ref vertices, ref normals, ref tangents);

                        var uniqueShapeName = $"{shapeName}{smr.name}";
                        newMesh.AddBlendShapeFrame(uniqueShapeName, weight, vertices, normals, tangents);
                    }
                }

                vertexCounter += smr.sharedMesh.vertexCount;
            }
        }


        private void FillByVertexCount(int vertexCount, int offset,
            List<Vector3> vertexList, List<Vector3> normalList, List<Vector3> tangentList,
            ref Vector3[] vertices, ref Vector3[] normals, ref Vector3[] tangents)
        {
            vertexList.Clear();
            normalList.Clear();
            tangentList.Clear();

            for (var k = 0; k < offset; k++)
            {
                vertexList.Add(Vector3.zero);
                normalList.Add(Vector3.zero);
                tangentList.Add(Vector3.zero);
            }

            vertexList.AddRange(vertices);
            normalList.AddRange(normals);
            tangentList.AddRange(tangents);

            var needFillCount = vertexCount - vertexList.Count;
            for (var k = 0; k < needFillCount; k++)
            {
                vertexList.Add(Vector3.zero);
                normalList.Add(Vector3.zero);
                tangentList.Add(Vector3.zero);
            }

            vertices = vertexList.ToArray();
            normals = normalList.ToArray();
            tangents = tangentList.ToArray();
        }

        public bool TryGetBoneByName(string boneName, out Transform bone)
        {
            return _bonesMap.TryGetValue(boneName, out bone);
        }

        private void DestroyCompat(GameObject obj)
        {
#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
    }
}