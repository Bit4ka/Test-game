using UnityEngine;

//The MIT License (MIT)

//Copyright(c) 2016 Ha.Minh and Modified by Mayke Rodrigues

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
namespace MaykerStudio.Demo
{
    public class Parallax : MonoBehaviour
    {
        public float speedX;
        public float speedY;
        public float Smoothing = 1000f;
        public bool moveInOppositeDirection;
        public bool moveParallax = true;

        private Transform cameraTransform;
        private Vector3 previousCameraPosition;

        void OnEnable()
        {
            GameObject gameCamera = Camera.main.gameObject;
            cameraTransform = gameCamera.transform;
            previousCameraPosition = cameraTransform.position;
        }

        void LateUpdate()
        {
            if (!moveParallax)
                return;

            float direction = (moveInOppositeDirection) ? -1f : 1f;
            Vector3 distance = (cameraTransform.position - previousCameraPosition) * new Vector2(speedX, speedY) * direction;
            Vector3 targetPos = new Vector3(transform.position.x + distance.x, transform.position.y + distance.y, transform.position.z);

            transform.position = Vector3.Lerp(transform.position, targetPos, Smoothing * Time.unscaledDeltaTime);

            previousCameraPosition = cameraTransform.position;
        }
    }

}
