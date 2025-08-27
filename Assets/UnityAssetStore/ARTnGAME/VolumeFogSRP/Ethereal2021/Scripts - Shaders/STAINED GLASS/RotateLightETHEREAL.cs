using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artngame.BrunetonsAtmosphere
{
    public class RotateLightETHEREAL : MonoBehaviour
    {

        public bool lookTarget = false;
        public Transform target;

        public float speed = 5.0f;

        private Vector3 lastMousePos;

        private bool rotate;

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) rotate = true;
            if (Input.GetMouseButtonUp(0)) rotate = false;

            Vector3 delta = lastMousePos - Input.mousePosition;

            if (rotate)
            {
                if (lookTarget)
                {
                    transform.Translate (new Vector3(0, delta.y * Time.deltaTime * -speed, 0));
                    transform.Translate(new Vector3(delta.x * Time.deltaTime * -speed,0, 0));
                    transform.LookAt(target);
                }
                else
                {
                    transform.Rotate(new Vector3(delta.y * Time.deltaTime * -speed, delta.x * Time.deltaTime * -speed, 0));
                }
            }

            lastMousePos = Input.mousePosition;
        }
    }
}
