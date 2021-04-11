﻿//See Vector.cs before
//See Material.cs before
    //See Texture32.cs before

#define DEVELOPMENT

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace GLTech2
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Wall_
    {
        internal Vector geom_direction;
        internal Vector geom_start;
        internal Material_ material; //Propositalmente por valor.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Wall_* Alloc(Vector start, Vector end, Material material)
        {
            Wall_* result = (Wall_*)Marshal.AllocHGlobal(sizeof(Wall_));
            result->material = *material.unmanaged;
            result->geom_direction = end - start;
            result->geom_start = start;
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Wall_* Alloc(Vector start, float angle, float length, Material material)
        {
            Wall_* result = (Wall_*)Marshal.AllocHGlobal(sizeof(Wall_));
            Vector dir = new Vector(angle) * length;
            result->material = *material.unmanaged;
            result->geom_direction = dir;
            result->geom_start = start;
            return result;
        }
    }

    public unsafe class Wall : IDisposable
    {
        [SecurityCritical]
        internal Wall_* unmanaged;
        internal Material refMaterial;
        internal bool isBound = false;
        #region Constructors
        public Wall(Vector start, Vector end, Material material)
        {
            unmanaged = Wall_.Alloc(start, end, material);
            refMaterial = material;
        }

        public Wall(Vector start, float angle_deg, Material material)
        {
            unmanaged = Wall_.Alloc(start, angle_deg, 1f, material);
            refMaterial = material;
        }
        
        public Wall(Vector start, float angle_deg, float length, Material material)
        {
            unmanaged = Wall_.Alloc(start, angle_deg, length, material);
            refMaterial = material;
        }
        #endregion

        #region Properties
        public float Angle
        {
            get => unmanaged->geom_direction.Angle;
            set
            {
                unmanaged->geom_direction = unmanaged->geom_direction.Module * new Vector(value);
            }
        }

        public float Length
        {
            get => unmanaged->geom_direction.Module;
            set => unmanaged->geom_direction *= value / unmanaged->geom_direction.Module;
        }

        public Material Material
        {
            get => refMaterial;
            set
            {
                refMaterial = value;
                unmanaged->material = value;
            }
        }

        public Vector Start
        {
            get => unmanaged->geom_start;
            set => unmanaged->geom_start = value;
        }

        public Vector Direction
        {
            get => unmanaged->geom_direction;
            set => unmanaged->geom_direction = value;
        }

        public Vector End
        {
            get => unmanaged->geom_start + unmanaged->geom_direction;
            set => unmanaged->geom_direction = value - unmanaged->geom_start;
        }
        #endregion

        #region Methods

        public static Wall[] FromBitmap(Bitmap source, params Color[] ignoreList)
        {
            if (ignoreList.Length == 0)
                throw new ArgumentException("It doesn't make sense to create a set of walls from a bitmap without having an ignore list. You're probably missing it.");

            Wall[] walls = new Wall[4 * source.Width * source.Height];
            int[] ignoreArgb = new int[ignoreList.Length];
            int index = 0;

            //Caches the argb of every color.
            foreach (Color color in ignoreList)
                ignoreArgb[index++] = color.ToArgb();

            index = 0;
            for (int column = 0; column < source.Width; column++)
            {
                for (int line = 0; line < source.Width; line++)
                {
                    int srcArgb = source.GetPixel(line, column).ToArgb();

                    if (ignoreArgb.Contains(srcArgb)) continue;
                    else
                    {
                        Texture32 blockTexture;
                        using (Bitmap blockBitmap = new Bitmap(1, 1))
                        {
                            blockBitmap.SetPixel(0, 0, source.GetPixel(line, column));
                            blockTexture = new Texture32(blockBitmap);
                        }
                        Vector vert1 = new Vector(line, -column);
                        Vector vert2 = new Vector(line, -column - 1);
                        Vector vert3 = new Vector(line + 1, -column - 1);
                        Vector vert4 = new Vector(line + 1, -column);
                        walls[index++] = new Wall(vert1, vert2, blockTexture);
                        walls[index++] = new Wall(vert2, vert3, blockTexture);
                        walls[index++] = new Wall(vert3, vert4, blockTexture);
                        walls[index++] = new Wall(vert4, vert1, blockTexture);
                    }
                }
            }
            return walls;
        }

        public static Wall[] FromBitmap(Bitmap source, IDictionary<int, Material> materials)
        {
            Wall[] walls = new Wall[4 * source.Width * source.Height];
            int index = 0;
            for (int srcColumn = 0; srcColumn < source.Width; srcColumn++)
            {
                for (int srcLine = 0; srcLine < source.Width; srcLine++)
                {
                    int srcArgb = source.GetPixel(srcLine, srcColumn).ToArgb();

                    if (materials.TryGetValue(srcArgb, out var material))
                    {
                        Vector vert1 = new Vector(srcLine, -srcColumn);
                        Vector vert2 = new Vector(srcLine, -srcColumn - 1);
                        Vector vert3 = new Vector(srcLine + 1, -srcColumn - 1);
                        Vector vert4 = new Vector(srcLine + 1, -srcColumn);
                        walls[index++] = new Wall(vert1, vert2, material);
                        walls[index++] = new Wall(vert2, vert3, material);
                        walls[index++] = new Wall(vert3, vert4, material);
                        walls[index++] = new Wall(vert4, vert1, material);
                    }
                }
            }
            return walls;
        }

        [Obsolete("Creating walls without can lead to errors.", true)]
        public static Wall[] GetWalls(params Vector[] verts)
        {
            if (verts == null)
                throw new ArgumentException("Verts cannot be null.");
            if (verts.Length <= 1)
                throw new ArgumentException("At least two verts should be passed.");

            Wall[] result = new Wall[verts.Length - 1];
            result[0] = new Wall(verts[0], verts[1], null);
            for (int i = 1; i < result.Length; i++)
                result[i] = new Wall(verts[i], verts[i + 1], null);
            return result;

        }

        [Obsolete]
        public static Wall[] GetWalls(Texture32 texture, bool stretch, params Vector[] verts)
        {
            if (verts == null)
                throw new ArgumentNullException("Verts cannot be null.");
            if (verts.Length <= 1)
                throw new ArgumentException("At least two verts should be passed.");

            Wall[] result = new Wall[verts.Length - 1];
            if (stretch)
            {
                result[0] = new Wall(verts[0], verts[1], texture);
                result[0].unmanaged->material.hrepeat = 1f / verts.Length;
                result[0].unmanaged->material.hoffset = 0f;
                for (int i = 1; i < result.Length; i++)
                {
                    result[i] = new Wall(verts[i], verts[i + 1], texture);
                    result[i].unmanaged->material.hrepeat = 1f / verts.Length;
                    result[i].unmanaged->material.hoffset = (float)i / verts.Length;
                }
                return result;
            }
            else
            {
                result[0] = new Wall(verts[0], verts[1], texture);
                for (int i = 1; i < result.Length; i++)
                    result[i] = new Wall(verts[i], verts[i + 1], texture);
                return result;
            }
        }

        public static Wall[] CreateSequence(Material material, params Vector[] verts)
        {
            if (verts == null)
                throw new ArgumentNullException("Verts cannot be null.");
            if (verts.Length <= 1)
                return new Wall[0];

            Wall[] result = new Wall[verts.Length - 1];
            Material_ material_ = *material.unmanaged;
            int walls = verts.Length - 1;

            float material_voffset = material_.voffset;
            float material_vrepeat = material_.vrepeat;
            float material_hrepeat = material_.hrepeat / walls;

            for (int i = 0; i < walls; i++)
            {
                float material_hoffset = material_.hoffset + material_.hrepeat * i / walls;
                Material currentMaterial = new Material(material.Texture, material_hoffset, material_hrepeat);
                result[i] = new Wall(verts[i], verts[i + 1], currentMaterial);
            }

            return result;
        }

        public static Wall[] CreatePolygon(Material material, params Vector[] verts) //Beta
        {
            if (verts == null)
                throw new ArgumentNullException("Verts cannot be null.");
            if (verts.Length <= 1)
                return new Wall[0];

            Wall[] result = new Wall[verts.Length];
            Material_ material_ = *material.unmanaged;
            int total_walls = verts.Length;

            float material_voffset = material_.voffset;
            float material_vrepeat = material_.vrepeat;
            float material_hrepeat = material_.hrepeat / total_walls;
            float material_hoffset;
            Material currentMaterial;

            for (int i = 0; i < total_walls - 1; i++)
            {
                material_hoffset = material_.hoffset + material_.hrepeat * i / (total_walls);
                currentMaterial = new Material(material.Texture, material_hoffset, material_hrepeat);
                result[i] = new Wall(verts[i], verts[i + 1], currentMaterial);
            }

            material_hoffset = material_.hoffset + material_.hrepeat * (total_walls - 1) / total_walls;
            currentMaterial = new Material(material.Texture, material_hoffset, material_hrepeat);
            result[total_walls - 1] = new Wall(verts[total_walls - 1], verts[0], currentMaterial);

            return result;
        }

        [Obsolete]
        public static Wall[] GetRegularPolygon(Vector center, float radius, int edges, Texture32 texture)
        {
            Vector[] vectors = new Vector[edges];
            Wall[] walls = new Wall[edges];
            for (int i = 0; i < edges; i++)
                vectors[i] = center + radius * new Vector(i * 360 / edges);
            for (int i = 0; i < edges - 1; i++)
            {
                walls[i] = new Wall(vectors[i], vectors[i + 1], texture);
                walls[i].unmanaged->material.hrepeat = 1f / edges;
                walls[i].unmanaged->material.hoffset = (float)i / edges;
            }
            walls[edges - 1] = new Wall(vectors[edges - 1], vectors[0], texture);
            walls[edges - 1].unmanaged->material.hrepeat = 1f / edges;
            walls[edges - 1].unmanaged->material.hoffset = (float)(edges - 1) / edges;
            return walls;
        }

        [Obsolete]
        public static Wall[] GetRegularPolygon(Vector center, float radius, int edges, Material material)
        {
            Vector[] polygon = Vector.GetPolygon(center, radius, edges);
            Wall[] result = CreateSequence(material, polygon);
            return result;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr) unmanaged);
        }

        ~Wall()
        {
            this.Dispose();
        }
        #endregion
    }
}
