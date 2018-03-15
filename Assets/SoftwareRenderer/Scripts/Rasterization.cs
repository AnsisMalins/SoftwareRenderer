using UnityEngine;

// http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html

public static class Rasterization
{
    public static
#if FILL_TRIANGLE_TEST
        System.Collections.Generic.IEnumerable<System.Tuple<int, int, Color32>>
#else
        void
#endif
        FillTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 color, Color32[] colorBuffer,
        float[] depthBuffer, int stride, int top, int bottom, int left, int right)
    {
        // Sort vertices vertically
        if (a.y > c.y) { Vector3 t = a; a = c; c = t; }
        if (a.y > b.y) { Vector3 t = a; a = b; b = t; }
        if (b.y > c.y) { Vector3 t = b; b = c; c = t; }

        float ac_y = c.y - a.y;

        if (ac_y < Mathf.Epsilon)
#if FILL_TRIANGLE_TEST
            yield break;
#else
            return; // Degenerate triangle
#endif

        // Slope from A to C of x over y. In other words, the rate of change of x, as y changes
        // from Ay to Cy.
        float dACxy = (c.x - a.x) / ac_y;

        float ab_y = b.y - a.y;

        // Find point D on AC, such that Dx equals Bx. This splits the triangle horizontally.
        float d_x = a.x + dACxy * ab_y;

        float bd_x = d_x - b.x;

        if (Mathf.Abs(bd_x) < Mathf.Epsilon)
#if FILL_TRIANGLE_TEST
            yield break;
#else
            return; // Degenerate triangle
#endif

        float dACzy = (c.z - a.z) / ac_y;

        float d_z = a.z + dACzy * ab_y;

        float dBDzx = (d_z - b.z) / bd_x;
        // Not sure why this works out.
        float dLRzx = dBDzx; //b.x < d_x ? -dBDzx : dBDzx;

        if (ab_y > Mathf.Epsilon)
        {
            float dABxy = (b.x - a.x) / ab_y;
            float dABzy = (b.z - a.z) / ab_y;

            int ymin = Mathf.Clamp(Mathf.CeilToInt(a.y - 0.5f), top, bottom);
            int ymax = Mathf.Clamp(Mathf.CeilToInt(b.y - 0.5f), top, bottom);

            // Vertical distance from the vertex to the middle of the first row of pixels to fill.
            float a0_y = ymin - a.y + 0.5f;

            // Find out which slope is on the left, and which one is on the right. Then, we can
            // always color pixels from left to right.
            float dLxy, dRxy, dLzy, dRzy, l_x, r_x, l_z, r_z;
            if (dABxy < dACxy)
            {
                dLxy = dABxy;
                dLzy = dABzy;
                dRxy = dACxy;
                dRzy = dACzy;
            }
            else
            {
                dLxy = dACxy;
                dLzy = dACzy;
                dRxy = dABxy;
                dRzy = dABzy;
            }

            l_x = a.x + a0_y * dLxy - 0.5f;
            l_z = a.z + a0_y * dLzy;
            r_x = a.x + a0_y * dRxy - 0.5f;
            r_z = a.z + a0_y * dRzy;

            int jmin = ymin * stride;
            int jmax = ymax * stride;

            for (int j = jmin; j < jmax; j += stride)
            {
                // Optimization: The -0.5f has been hoisted out of the loop.
                int xmin = Mathf.Clamp(Mathf.CeilToInt(l_x), left, right);
                int xmax = Mathf.Clamp(Mathf.CeilToInt(r_x), left, right);
                int imin = j + xmin;
                int imax = j + xmax;

                float z = l_z;

                for (int i = imin; i < imax; i++)
                {
#if !FILL_TRIANGLE_TEST
                    if (z < depthBuffer[i])
#endif
                    {
                        depthBuffer[i] = z;
                        colorBuffer[i] = color;
#if FILL_TRIANGLE_TEST
                        yield return System.Tuple.Create(i % stride, i / stride, color);
#endif
                    }

                    z += dLRzx;
                }

                l_x += dLxy;
                r_x += dRxy;
                l_z += dLzy;
                r_z += dRzy;
            }
        }

        float bc_y = c.y - b.y;
        if (bc_y > Mathf.Epsilon)
        {
            float dBCxy = (c.x - b.x) / bc_y;
            float dBCzy = (c.z - b.z) / bc_y;

            int ymin = Mathf.Clamp(Mathf.CeilToInt(b.y - 0.5f), top, bottom);
            int ymax = Mathf.Clamp(Mathf.CeilToInt(c.y - 0.5f), top, bottom);

            float b0_y = ymin - b.y + 0.5f;

            float dLxy, dRxy, dLzy, dRzy, l_x, r_x, l_z, r_z;
            if (dACxy < dBCxy)
            {
                dLxy = dBCxy;
                dLzy = dBCzy;
                dRxy = dACxy;
                dRzy = dACzy;
                l_x = b.x + b0_y * dLxy;
                l_z = b.z + b0_y * dLzy;
                r_x = d_x + b0_y * dRxy;
                r_z = d_z + b0_y * dRzy;
            }
            else
            {
                dLxy = dACxy;
                dLzy = dACzy;
                dRxy = dBCxy;
                dRzy = dBCzy;
                l_x = d_x + b0_y * dLxy;
                l_z = d_z + b0_y * dLzy;
                r_x = b.x + b0_y * dRxy;
                r_z = b.z + b0_y * dRzy;
            }

            int jmin = ymin * stride;
            int jmax = ymax * stride;

            for (int j = jmin; j < jmax; j += stride)
            {
                int xmin = Mathf.Clamp(Mathf.CeilToInt(l_x - 0.5f), left, right);
                int xmax = Mathf.Clamp(Mathf.CeilToInt(r_x - 0.5f), left, right);
                int imin = j + xmin;
                int imax = j + xmax;

                float z = l_z;

                for (int i = imin; i < imax; i++)
                {
#if !FILL_TRIANGLE_TEST
                    if (z < depthBuffer[i])
#endif
                    {
                        depthBuffer[i] = z;
                        colorBuffer[i] = color;
#if FILL_TRIANGLE_TEST
                        yield return System.Tuple.Create(i % stride, i / stride, color);
#endif
                    }

                    z += dLRzx;
                }

                l_x += dLxy;
                r_x += dRxy;
                l_z += dLzy;
                r_z += dRzy;
            }
        }
    }
}