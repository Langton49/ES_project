using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    public static class CharacterControllerUtils 
    {
        public static Vector3 GetNormalWithSphereCast(CharacterController characterController, LayerMask layerMask = default) 
        {
            Vector3 normal = Vector3.up;
            Vector3 centre = characterController.transform.position + characterController.center;
            float distance = characterController.height / 2f + characterController.stepOffset + 0.01f;

            RaycastHit hit;
            if (Physics.SphereCast(centre, characterController.radius, Vector3.down, out hit, distance, layerMask))
            {
                normal = hit.normal;
            }

            return normal;

        }
    }

}
