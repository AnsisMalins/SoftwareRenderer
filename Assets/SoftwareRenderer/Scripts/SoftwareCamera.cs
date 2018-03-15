using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Camera))]
public sealed class SoftwareCamera : MonoBehaviour
{
#if DEBUG
    public Shader replacementShader;
#endif

    private int bufferLock;
    private WaitCallback clearColorBuffer;
    private WaitCallback clearDepthBuffer;
    private Color32[] colorBuffer;
    private float[] depthBuffer;
    private int pixelHeight;
    private int pixelWidth;
    private Vector4[] planes;
    private Texture2D renderTexture;
    private Vector3[] vertexBuffer;

    public SoftwareCamera()
    {
        clearColorBuffer = new WaitCallback(ClearColorBuffer);
        clearDepthBuffer = new WaitCallback(ClearDepthBuffer);
        planes = new Vector4[6];
    }

#if DEBUG
    private void OnValidate()
    {
        var camera = GetComponent<Camera>();
        camera.ResetReplacementShader();
        if (replacementShader != null)
            camera.SetReplacementShader(replacementShader, null);
    }
#endif

    private void LateUpdate()
    {
        var camera = GetComponent<Camera>();

        Profiler.BeginSample("ClearBuffers");
        if (pixelWidth != camera.pixelWidth || pixelHeight != camera.pixelHeight)
        {
            pixelWidth = camera.pixelWidth;
            pixelHeight = camera.pixelHeight;
            int pixelCount = pixelWidth * pixelHeight;
            if (colorBuffer == null || colorBuffer.Length != pixelCount)
                colorBuffer = new Color32[pixelCount];
            if (depthBuffer == null || depthBuffer.Length != pixelCount)
                depthBuffer = new float[pixelCount];
        }
        else
        {
            while (bufferLock > 0)
                Thread.SpinWait(4);
        }
        Profiler.EndSample();

        // Race condition: if viewport was resized, we don't wait for buffer clear tasks to finish,
        // but if they were not scheduled by this point, they will clear the image being rendered!

        var worldToProjectionMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
        var worldToScreenMatrix = Matrix4x4.TRS(
            new Vector3(pixelWidth * 0.5f, pixelHeight * 0.5f, 0), Quaternion.identity,
            new Vector3(pixelWidth * 0.5f, pixelHeight * 0.5f, 1)) * worldToProjectionMatrix;

        Vector4 p1 = worldToProjectionMatrix.GetRow(0);
        Vector4 p2 = worldToProjectionMatrix.GetRow(1);
        Vector4 p3 = worldToProjectionMatrix.GetRow(2);
        Vector4 p4 = worldToProjectionMatrix.GetRow(3);
        planes[0] = p4 + p3;
        planes[1] = p4 - p3;
        planes[2] = p4 + p1;
        planes[3] = p4 - p1;
        planes[4] = p4 + p2;
        planes[5] = p4 - p2;
        for (int i = 0; i < planes.Length; i++)
            planes[i] /= ((Vector3)planes[i]).magnitude;

        for (int r = 0; r < SoftwareRenderer.renderers.Count; r++)
        {
            var renderer = SoftwareRenderer.renderers[r];
            var transform = renderer.transform;

            Profiler.BeginSample("Culling");
            Vector4 position4 = transform.position;
            position4.w = 1;
            if (Vector4.Dot(planes[0], position4) < -renderer.radius
                || Vector4.Dot(planes[1], position4) < -renderer.radius
                || Vector4.Dot(planes[2], position4) < -renderer.radius
                || Vector4.Dot(planes[3], position4) < -renderer.radius
                || Vector4.Dot(planes[4], position4) < -renderer.radius
                || Vector4.Dot(planes[5], position4) < -renderer.radius)
            {
                renderer.gizmoColor = Color.red;
                Profiler.EndSample();
                continue;
            }
            else
            {
                renderer.gizmoColor = Color.green;
                Profiler.EndSample();
            }

            var localToScreenMatrix = worldToScreenMatrix * transform.localToWorldMatrix;
            Vector3[] vertices = renderer.vertices;
            if (vertexBuffer == null || vertexBuffer.Length < vertices.Length)
                vertexBuffer = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertexBuffer[i] = localToScreenMatrix.MultiplyPoint(vertices[i]);

            // Optimization: get scale from localToWorldMatrix instead?
            Vector3 scale = transform.lossyScale;
            float flipFaces = Mathf.Sign(scale.x * scale.y * scale.z);

            Profiler.BeginSample("Drawing");
            int[] triangles = renderer.triangles;
            Color32 color32 = renderer.color;
            for (int t = 0; t < triangles.Length;)
            {
                Vector3 a = vertexBuffer[triangles[t++]];
                Vector3 b = vertexBuffer[triangles[t++]];
                Vector3 c = vertexBuffer[triangles[t++]];

                if (a.z > -1 && a.z < 1 && b.z > -1 && b.z < 1 && c.z > -1 && c.z < 1 &&
                    Vector3.Cross(b - a, c - a).z * flipFaces < 0)
                {
                    Rasterization.FillTriangle(a, b, c, color32,
                        colorBuffer, depthBuffer, pixelWidth, 0, pixelHeight, 0, pixelWidth);
                }
            }
            Profiler.EndSample();
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (renderTexture == null)
            renderTexture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGB24, false);
        else if (renderTexture.width != pixelWidth || renderTexture.height != pixelHeight)
            renderTexture.Resize(pixelWidth, pixelWidth);

        renderTexture.SetPixels32(colorBuffer);

        bufferLock = 2;
        ThreadPool.QueueUserWorkItem(clearColorBuffer);
        ThreadPool.QueueUserWorkItem(clearDepthBuffer);

        renderTexture.Apply();
        Graphics.Blit(renderTexture, dest);
    }

    private void ClearColorBuffer(object state)
    {
        memset(colorBuffer, new Color32());
        Interlocked.Decrement(ref bufferLock);
    }

    private void ClearDepthBuffer(object state)
    {
        memset(depthBuffer, 1f);
        Interlocked.Decrement(ref bufferLock);
    }

    // https://stackoverflow.com/a/13806014/456116
    private static void memset<T>(T[] array, T value)
    {
        int length = Mathf.Min(array.Length, 128);
        for (int i = 0; i < length; i++)
            array[i] = value;
        if (length == array.Length)
            return;

        if (typeof(T).IsPrimitive)
        {
            int sizeOfT = Marshal.SizeOf(typeof(T));
            int arraySize = array.Length * sizeOfT;
            int blockSize = length * sizeOfT;
            int offset = blockSize;
            while (offset < arraySize)
            {
                int count = Mathf.Min(blockSize, arraySize - offset);
                Buffer.BlockCopy(array, 0, array, offset, count);
                offset += blockSize;
                blockSize *= 2;
            }
        }
        else
        {
            int offset = length;
            while (offset < array.Length)
            {
                int count = Mathf.Min(length, array.Length - offset);
                Array.Copy(array, 0, array, offset, count);
                offset += length;
                length *= 2;
            }
        }
    }
}