using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core
{
    [RequireComponent(typeof(MeshManager))]
    public class MeshModifier : MonoBehaviour
    {
        public MeshManager meshManager;

        private Dictionary<MeshPart, GameObject> _currentMesh = new Dictionary<MeshPart, GameObject>()
        {
            { MeshPart.Hair, null },
            { MeshPart.Top, null },
            { MeshPart.Bottom, null },
            { MeshPart.Shoe, null },
            { MeshPart.Suit, null }
        };

        public void AddCloth(MeshPart meshPart, GameObject mesh)
        {
            _currentMesh[meshPart] = mesh;
            SetWhatWhich(meshPart, mesh);
            meshManager.CreateNewMesh();
        }

        public void RemoveCloth(MeshPart meshPart)
        {
            _currentMesh[meshPart] = null;
            SetWhatWhich(meshPart, null);
            meshManager.CreateNewMesh();
        }

        private void SetWhatWhich(MeshPart meshPart, GameObject mesh)
        {
            var currentWhich = GetNewWhich(meshPart, mesh);
            meshManager.whatMeshes = new List<GameObject>();
            foreach (var item in _currentMesh)
            {
                meshManager.whatMeshes.Add(item.Value);
            }

            meshManager.whichMeshes = new List<string>();
            foreach (var item in currentWhich)
            {
                meshManager.whichMeshes.Add(item);
            }
        }

        private List<string> GetNewWhich(MeshPart meshPart, GameObject mesh = null)
        {
            var list = new HashSet<string>();
            var meshWhich = WhichAndPart.PartWhichComparison[meshPart];
            for (int i = 0; i < meshWhich.Count; i++)
            {
                if (mesh == null)
                {
                    list.Remove(meshWhich[i]);
                }
                else
                {
                    list.Add(meshWhich[i]);
                }
            }

            return list.ToList();
        }
    }
}