using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Love.Misc
{
    public class TransformStack
    {
        public LinkedList<Matrix44?> list = new LinkedList<Matrix44?>();

        public TransformStack()
        {
            Push();
        }

        public void Pop()
        {
            if (list.Count == 1)
                throw new System.Exception(" stack is empty ! ");

            list.RemoveLast();
        }

        public void Push()
        {
            list.AddLast(Matrix44.Identity);
        }

        public void Scale(float sx, float sy)
        {
            var m = list.Last.Value.Value;
            list.AddLast(m * Matrix44.CreateScale(sx, sy, 1));
        }

        public void Translate(float ox, float oy)
        {
            var m = list.Last.Value.Value;
            list.AddLast(m * Matrix44.CreateTranslation(ox, oy, 0));
        }

        public void Rotate(float r)
        {
            var m = list.Last.Value;
            list.AddLast(m * Matrix44.CreateRotationZ(r));
        }

        public Vector2 TransformPoint(Vector2 p)
        {
            var v3 = TransformVector3(new Vector3(p, 0), list.Last.Value.Value);
            return new Vector2(v3.x, v3.y);
        }

        public Vector2 TransformPointInvert(Vector2 p)
        {
            Matrix44.Invert(list.Last.Value.Value, out var result);
            var v3 = TransformVector3(new Vector3(p, 0), result);
            return new Vector2(v3.x, v3.y);
        }


        public static Vector3 TransformVector3(Vector3 vector, Matrix44 matrix)
        {
            return new Vector3(
                vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + 1 * matrix.M41,
                vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + 1 * matrix.M42,
                vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + 1 * matrix.M43);
        }

    }
}