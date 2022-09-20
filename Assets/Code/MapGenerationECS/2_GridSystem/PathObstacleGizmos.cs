using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KWZTerrainECS
{
    public class PathObstacleGizmos : MonoBehaviour
    {
        public AuthoringGridSystem gridSystem;
        private EntityManager em;
        // Start is called before the first frame update
        private void Start()
        {
            em = gridSystem.entityManager;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
