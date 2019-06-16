using OpenTK;
using System;

namespace Silver.ModelViewer
{
    class Camera
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Orientation = new Vector3((float)Math.PI, 0f, 0f);
        public float MoveSpeed = 0.08f;
        public float MouseSensitivity = 0.01f;

        public Matrix4 GetScreenProjectionMatrix(float aspect)
        {
            return Matrix4.LookAt(Vector3.Zero, new Vector3(0, 0, -1), Vector3.UnitY) * Matrix4.CreateOrthographic(aspect * 100, 100, 0, 10f);
        }

        public Matrix4 GetViewProjectionMatrix(float aspect)
        {
            return GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(1.3f, aspect, .1f, 1000f);
        }

        /// <summary>
        /// Create a view matrix for this Camera
        /// </summary>
        /// <returns>A view matrix to look in the camera's direction</returns>
        public Matrix4 GetViewMatrix()
        {
            Vector3 lookat = new Vector3
            {
                X = (float)(Math.Sin(Orientation.X) * Math.Cos(Orientation.Y)),
                Y = (float)Math.Sin(Orientation.Y),
                Z = (float)(Math.Cos(Orientation.X) * Math.Cos(Orientation.Y))
            };
            return Matrix4.LookAt(Position, Position + lookat, Vector3.UnitY);
        }

        /// <summary>
        /// Offset the position of the camera in coordinates relative to its current orientation
        /// </summary>
        /// <param name="x">Movement along the camera ground (left/right)</param>
        /// <param name="y">Movement along the camera axis (forward)</param>
        /// <param name="z">Height to move</param>
        public void Move(float x, float y, float z)
        {
            Vector3 offset = new Vector3();
            Vector3 forward = new Vector3((float)Math.Sin((float)Orientation.X), 0, (float)Math.Cos((float)Orientation.X));
            Vector3 right = new Vector3(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, MoveSpeed);

            Position += offset;
        }

        /// <summary>
        /// Adds rotation from mouse movement to camera orientation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AddRotation(float x, float y)
        {
            x = x * MouseSensitivity;
            y = y * MouseSensitivity;

            Orientation.X = (Orientation.X + x) % ((float)Math.PI * 2.0f);
            Orientation.Y = Math.Max(Math.Min(Orientation.Y + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }
    }
}
